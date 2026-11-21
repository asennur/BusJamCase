#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BusJam.Core;
using BusJam.UI;
using DG.Tweening;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// <summary>Default-pose regression, independent step edits and live tween retiming in Edit mode.</summary>
public static class ResultTimelineReview
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
    private static int assertions;
    private static object Call(object value, string method, params object[] args) => value.GetType().GetMethod(method, Flags).Invoke(value, args);
    private static object Fixture(string method, params object[] args) => typeof(ResultScreenReview).GetMethod(method, Flags).Invoke(null, args);
    private static void Check(bool condition, string message) { assertions++; if (!condition) throw new InvalidOperationException(message); }
    private static void Equal(float actual, float expected, string name) => Check(Mathf.Abs(actual - expected) < .0002f, name + ": " + actual + " != " + expected);
    private static float Phase(float time, float start, float duration) => Mathf.Clamp01((time - start) / duration);
    private static float Pop(float p) { p -= 1; return 1 + 2.70158f * p * p * p + 1.70158f * p * p; }

    private static void Edit(ResultTimeline timeline, ResultStepId id, float start, float duration, bool enabled = true)
    {
        var steps = (List<ResultStep>)typeof(ResultTimeline).GetField("steps", Flags).GetValue(timeline);
        for (int i = 0; i < steps.Count; i++)
        {
            if (steps[i].id != id) continue;
            var step = steps[i]; step.start = start; step.duration = duration; step.enabled = enabled;
            steps[i] = step; timeline.Rebuild(); return;
        }
        throw new InvalidOperationException("Missing step " + id);
    }

    public static string Verify()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) throw new InvalidOperationException("Compiled Edit mode required.");
        assertions = 0;
        Fixture("Create", 1080, 1920);
        var game = (ToolkitGameUI)typeof(ResultScreenReview).GetField("game", Flags).GetValue(null);
        var document = game.GetComponent<UIDocument>();
        var root = document.rootVisualElement;
        var timeline = Object.Instantiate(AssetDatabase.LoadAssetAtPath<ResultTimeline>("Assets/_BusJam/Data/ResultTimeline.asset"));
        timeline.hideFlags = HideFlags.HideAndDontSave;
        typeof(ToolkitGameUI).GetField("resultTimeline", Flags).SetValue(game, timeline);
        try
        {
            Equal(timeline.Length(ResultTrack.Complete), 3.45f, "Complete length");
            Equal(timeline.Length(ResultTrack.Collection), 1.8f, "Collection length");
            Equal(timeline.Length(ResultTrack.Fail), 1.3f, "Fail length");
            Equal(timeline.Length(ResultTrack.Retry), .4f, "Retry length");
            game.DisplayState(GameState.Complete);
            for (int frame = 0; frame <= 69; frame++)
            {
                float time = frame * .05f;
                Call(game, "SetCelebrationTime", time);
                Equal(root.Q("complete-overlay").style.backgroundColor.value.a, .88f * Phase(time,1.05f,.2f), "Overlay alpha");
                Equal(root.Q("complete-title-group").style.opacity.value, time < 1.75f ? 0 : 1, "Title visibility");
                Equal(root.Q("complete-title-group").style.scale.value.value.x, Mathf.Max(.01f,Pop(Phase(time,1.75f,.3f))), "Title scale");
                Equal(root.Q("complete-rays").style.opacity.value, .8f * Phase(time,1.75f,.3f), "Rays alpha");
                Equal(root.Q("reward-pile").style.scale.value.value.x, Mathf.Lerp(.15f,1,Pop(Phase(time,2.5f,.3f))), "Pile scale");
                float count = Phase(time,2.5f,.9f);
                Check(root.Q<Label>("reward-label").text == Mathf.RoundToInt(25*(1-(1-count)*(1-count))).ToString(), "Coin count");
                Equal(root.Q("continue-button").style.opacity.value, time < 2.6f ? 0 : 1, "Continue visibility");
            }
            game.DisplayState(GameState.Fail);
            for (int frame = 0; frame <= 26; frame++)
            {
                float time = frame*.05f; Call(game,"SetFailureEntry",time);
                Equal(root.Q("fail-overlay").style.backgroundColor.value.a, .72f*Phase(time,.6f,.2f), "Fail overlay");
                Equal(root.Q("fail-panel").style.opacity.value, Phase(time,.6f,.2f), "Fail panel alpha");
                Equal(root.Q("fail-panel").style.scale.value.value.x,.86f+.14f*Pop(Phase(time,.6f,.2f)),"Fail panel scale");
            }

            Edit(timeline,ResultStepId.Title,4.2f,.5f);
            Edit(timeline,ResultStepId.ContinueButton,.5f,.1f);
            game.DisplayState(GameState.Complete); Call(game,"SetCelebrationTime",.6f);
            Equal(root.Q("complete-title-group").style.opacity.value,0,"Delayed title");
            Check(root.Q<Button>("continue-button").enabledSelf,"Continue can appear before title");
            Call(game,"SetCelebrationTime",4.3f);
            Equal(root.Q("complete-title-group").style.opacity.value,1,"Edited title appears");
            Equal(timeline.Length(ResultTrack.Complete),4.7f,"Derived edited length");
            Edit(timeline,ResultStepId.CoinPile,0,0,false);
            Edit(timeline,ResultStepId.CoinCount,0,0,false);
            Edit(timeline,ResultStepId.Overlay,0,0,false);
            Edit(timeline,ResultStepId.Confetti,0,0,false);
            Call(game,"SetCelebrationTime",1f);
            Equal(root.Q("reward-pile").style.opacity.value,0,"Disabled pile");
            Equal(root.Q("reward-label").style.opacity.value,0,"Disabled count");
            Equal(root.Q("complete-overlay").style.backgroundColor.value.a,0,"Disabled overlay");
            foreach (var particle in (VisualElement[])typeof(ToolkitGameUI).GetField("confetti",Flags).GetValue(game)) Equal(particle.style.opacity.value,0,"Disabled confetti");
            Edit(timeline,ResultStepId.CoinFlight,0,0,false);
            Call(game,"ContinueLevel");Call(game,"SetCollectionTime",timeline.Length(ResultTrack.Collection));
            Check((bool)typeof(ToolkitGameUI).GetField("rewardCredited",Flags).GetValue(game),"Disabled flight still settles reward once");

            Edit(timeline,ResultStepId.BoardHold,0,2);
            Equal(timeline.Get(ResultStepId.Panel).start,2,"BoardHold gates entry");
            Equal(timeline.Get(ResultStepId.LifeLoss).start,2,"BoardHold gates life animation");
            Edit(timeline,ResultStepId.LifeLoss,2,.6f,false);
            game.DisplayState(GameState.Fail); Call(game,"SetFailureEntry",3f);
            Equal(root.Q("fail-effects").ElementAt(0).style.opacity.value,0,"Disabled life float");
            Edit(timeline,ResultStepId.Panel,3,.4f);Call(game,"SetFailureEntry",3.2f);
            Check(!root.Q<Button>("retry-button").enabledSelf,"Retry waits for edited panel");
            Call(game,"SetFailureEntry",3.4f);Check(root.Q<Button>("retry-button").enabledSelf,"Retry unlocks at edited panel end");

            var zero = new ResultStep { enabled=true, start=1, duration=0 };
            Equal(zero.Phase(.9f),0,"Zero-duration before");Equal(zero.Phase(1),1,"Zero-duration at start");
            Edit(timeline,ResultStepId.Title,float.NaN,float.PositiveInfinity);
            Check(!float.IsNaN(timeline.Length(ResultTrack.Complete))&&!float.IsInfinity(timeline.Length(ResultTrack.Complete)),"Nonfinite edits sanitized");
            Edit(timeline,ResultStepId.CoinFlight,1,1);
            Equal(timeline.Length(ResultTrack.Collection),2.455f,"Collection includes final arrival punch");
            CheckRetiming();
            string report = "PASS: "+assertions+" assertions. Default Complete/Fail poses match previous formulas at 0.05-second intervals. Edited order, duration, disabled steps, BoardHold gating, zero duration, nonfinite values, reward settlement without CoinFlight and live tween retiming passed. Only a temporary asset clone was edited; no Play or progress writes.\n";
            File.WriteAllText(VectorUiReview.Folder+"/timeline-validation.txt",report);return report;
        }
        finally { Fixture("Cleanup");Object.DestroyImmediate(timeline); }
    }

    private static void CheckRetiming()
    {
        float value=0;
        var tween=DOTween.To(()=>0f,x=>value=x,3.45f,3.45f).SetEase(Ease.Linear).SetAutoKill(false).Pause();
        try
        {
            tween.Goto(2,false);
            typeof(ToolkitGameUI).GetMethod("Retime",Flags).Invoke(null,new object[]{tween,5f,true});
            Equal(tween.Duration(),5,"Live duration");Equal(tween.Elapsed(),2,"Live edit preserves elapsed seconds");
            Check(tween.IsPlaying(),"Live edit resumes active result");Equal(value,2,"Live edit preserves sampled time");
        }
        finally
        {
            var manager=typeof(DOTween).Assembly.GetType("DG.Tweening.Core.TweenManager");
            manager.GetMethod("Despawn",Flags).Invoke(null,new object[]{tween,true});
        }
    }

    public static string VerifyPreviewLifecycle()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling) throw new InvalidOperationException("Edit mode required.");
        foreach(var old in Resources.FindObjectsOfTypeAll<ResultTimelinePreview>()) old.Close();
        PreviewCycle(false);
        int textures=OwnedTextures(),objects=OwnedObjects();
        int scenes=UnityEditor.SceneManagement.EditorSceneManager.previewSceneCount;
        for(int i=0;i<12;i++) PreviewCycle(i==11);
        int textureGrowth=OwnedTextures()-textures,objectGrowth=OwnedObjects()-objects;
        int sceneGrowth=UnityEditor.SceneManagement.EditorSceneManager.previewSceneCount-scenes;
        Check(textureGrowth==0&&objectGrowth==0&&sceneGrowth==0,"Preview resource growth");
        string report="PASS: 12 open/sample/scrub/resize/close cycles, all four tracks, three sizes. Owned RenderTexture growth="+textureGrowth+", owned GameObject growth="+objectGrowth+", preview scene growth="+sceneGrowth+". No Play or scene/progress saves.\n";
        File.WriteAllText(VectorUiReview.Folder+"/preview-lifecycle.txt",report);return report;
    }

    private static int OwnedTextures()
    {
        int count=0;
        foreach(var item in Resources.FindObjectsOfTypeAll<RenderTexture>())
            if(item.name=="Result Timeline Preview"||item.name=="Celebration Screen Target")count++;
        return count;
    }
    private static int OwnedObjects()
    {
        int count=0;
        foreach(var item in Resources.FindObjectsOfTypeAll<GameObject>())
            if(item.name=="Result Timeline Preview"||item.name=="Result Preview World Camera"||item.name=="Complete Celebration Pool")count++;
        return count;
    }
    private static void PreviewCycle(bool save)
    {
        ResultTimelinePreview.OpenMenu();
        var window=Resources.FindObjectsOfTypeAll<ResultTimelinePreview>()[0];
        try
        {
            for(int size=0;size<3;size++)
            {
                Call(window,"Cleanup");typeof(ResultTimelinePreview).GetField("sizeIndex",Flags).SetValue(window,size);
                foreach(ResultTrack track in Enum.GetValues(typeof(ResultTrack)))
                {
                    Call(window,"Play",track);
                    typeof(ResultTimelinePreview).GetField("playing",Flags).SetValue(window,false);
                    foreach(float time in new[]{0f,.3f,1.85f,.6f,3.45f})
                    {
                        typeof(ResultTimelinePreview).GetField("time",Flags).SetValue(window,time);
                        Call(window,"Tick");
                        if(size==2)
                        {
                            var doc=(UIDocument)typeof(ResultTimelinePreview).GetField("document",Flags).GetValue(window);
                            Check(doc.rootVisualElement.Q("ui-root").ClassListContains("landscape"),"Preview uses its own landscape viewport");
                        }
                    }
                    if(save&&size==0)
                    {
                        typeof(ResultTimelinePreview).GetField("time",Flags).SetValue(window,track==ResultTrack.Complete?1.85f:track==ResultTrack.Fail?1.3f:track==ResultTrack.Collection?.6f:.2f);
                        Call(window,"Tick");
                        var target=(RenderTexture)typeof(ResultTimelinePreview).GetField("target",Flags).GetValue(window);
                        VectorUiReview.Save(target,VectorUiReview.Folder+"/timeline-"+track.ToString().ToLowerInvariant()+".png");
                    }
                }
            }
        }
        finally{window.Close();}
    }
}
#endif
