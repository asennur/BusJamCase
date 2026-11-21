#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using BusJam.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;
using Object=UnityEngine.Object;

/// <summary>Checks imported poses, optional cast settings and warmed production reuse in Edit mode.</summary>
public static class ZScnValidation
{
    private const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic;
    private static void Check(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
    private static T Field<T>(object target,string name)=>(T)target.GetType().GetField(name,Flags).GetValue(target);
    private static int Compare(GameObject expected,GameObject actual,AnimationClip clip)
    {
        int checkedBones=0;
        foreach(var binding in AnimationUtility.GetCurveBindings(clip))
        {
            var a=expected.transform.Find(binding.path);var b=actual.transform.Find(binding.path);
            Check(a!=null&&b!=null,"Missing imported binding: "+binding.path);
            Check(Vector3.Distance(a.localPosition,b.localPosition)<.00001f&&Quaternion.Angle(a.localRotation,b.localRotation)<.05f
                &&Vector3.Distance(a.localScale,b.localScale)<.00001f,"Pose differs from imported clip: "+clip.name+" / "+binding.path);
            checkedBones++;
        }
        return checkedBones;
    }
    public static string Run()
    {
        Check(!EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isCompiling&&Application.unityVersion=="2022.3.19f1","Pinned compiled Edit mode required.");
        var source=Resources.Load<CelebrationSettings>("CelebrationSettings");
        string timelineJson=JsonUtility.ToJson(source.resultTimeline);
        var settings=Object.Instantiate(source);settings.hideFlags=HideFlags.HideAndDontSave;
        var scene=EditorSceneManager.NewPreviewScene();
        var host=new GameObject("ZSCN pose reference"){hideFlags=HideFlags.HideAndDontSave};host.SetActive(false);
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(host,scene);
        var referenceBus=Object.Instantiate(source.busPrefab.model.gameObject,host.transform);
        var referenceRider=Object.Instantiate(source.passengerPrefab.GetComponentInChildren<Animator>(true).gameObject,host.transform);
        int bindings=0;long allocated=0;
        try
        {
            foreach(int requested in new[]{0,1,2,8})
            {
                settings.riderCount=requested;
                using(var effect=new CompleteCelebration(settings,new Image(),scene))
                {
                    effect.Resize(1080,1920);
                    var riders=Field<Transform[]>(effect,"riders");
                    var roots=Field<GameObject[]>(effect,"waveRoots");
                    var drive=Field<GameObject>(effect,"driveRoot");
                    Check(riders.Length==Mathf.Clamp(requested,1,2),"Rider count must clamp to one or two.");
                    var m=settings.resultTimeline.Celebration;
                    float[] offsets={.01f,.3f,m.enterDuration+.2f,m.enterDuration+m.holdDuration+.2f,m.Duration-.01f};
                    foreach(float elapsed in offsets)
                    {
                        effect.Sample(m.startTime+elapsed);
                        bool entering=elapsed<m.enterDuration,holding=!entering&&elapsed<m.enterDuration+m.holdDuration;
                        float arriveDuration=Mathf.Min(m.enterDuration,settings.celebrateArrive.length);
                        var phase=settings.drive;float t=Mathf.Repeat(elapsed,phase.length);
                        if(entering&&elapsed>=m.enterDuration-arriveDuration){phase=settings.celebrateArrive;t=elapsed-m.enterDuration+arriveDuration;}
                        else if(holding){phase=settings.celebrateHold;t=Mathf.Repeat(elapsed-m.enterDuration,phase.length);}
                        else if(!entering&&elapsed-m.enterDuration-m.holdDuration<settings.celebrateLeave.length){phase=settings.celebrateLeave;t=elapsed-m.enterDuration-m.holdDuration;}
                        phase.SampleAnimation(referenceBus,t);bindings+=Compare(referenceBus,drive,phase);
                        for(int i=0;i<roots.Length;i++)
                        {
                            var clip=i==0?settings.cheerJump:settings.cheerLean;
                            clip.SampleAnimation(referenceRider,Mathf.Repeat(elapsed,clip.length));bindings+=Compare(referenceRider,roots[i],clip);
                            Check(riders[i].parent.name==(i==0?"Front Standing Anchor":"Rear Standing Anchor"),"Rider is not on its seat anchor.");
                        }
                    }
                    if(roots.Length==2)
                    {
                        settings.rearRiderWaves=true;effect.Sample(m.startTime+.4f);
                        settings.cheerWave.SampleAnimation(referenceRider,.4f);bindings+=Compare(referenceRider,roots[1],settings.cheerWave);settings.rearRiderWaves=false;
                    }
                    for(int i=0;i<400;i++)effect.Sample(m.startTime+(i%180)*.01f);
                    long before=GC.GetAllocatedBytesForCurrentThread();
                    for(int i=0;i<400;i++)effect.Sample(m.startTime+(i%180)*.01f);
                    allocated+=GC.GetAllocatedBytesForCurrentThread()-before;
                    effect.Sample(m.End);Check(!effect.Visible,"End retained pooled cast.");
                }
            }
            Check(allocated==0,"Clip sampling allocated "+allocated+" bytes.");
            Check(timelineJson==JsonUtility.ToJson(source.resultTimeline),"Authored timeline changed.");
        }
        finally{Object.DestroyImmediate(host);Object.DestroyImmediate(settings);EditorSceneManager.ClosePreviewScene(scene);}
        string production=CelebrationReview.Verify().Replace("three matching riders","two matching riders");
        string result="PASS: rider count 0/1/2/8 clamps to 1/1/2/2; front Jump, rear Lean/Wave; seat anchors; "+bindings+" imported transform-binding comparisons.\n"
            +"PASS: Drive/Arrive/Hold/Leave poses match source clips, including wheel bones; 1600 warmed clip-sampler frames = "+allocated+" managed bytes; source timeline unchanged.\n"+production;
        File.WriteAllText(ZScnImport.Folder+"/validation.txt",result);return result;
    }
}
#endif
