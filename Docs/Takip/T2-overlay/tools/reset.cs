var fl=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
var t=typeof(BusJam.Gameplay.CameraFitter);
var go=new GameObject("tmp"); go.hideFlags=HideFlags.HideAndDontSave; var fresh=go.AddComponent<BusJam.Gameplay.CameraFitter>();
var sb=new System.Text.StringBuilder();
foreach(var f in UnityEngine.Object.FindObjectsOfType<BusJam.Gameplay.CameraFitter>())
 foreach(var n in new[]{"busRoofLocal","busRoofTop","gridAboveBar","referenceColumns","referenceRows","sideMargin","slotRowLocal"}){var fi=t.GetField(n,fl);fi.SetValue(f,fi.GetValue(fresh));sb.Append(n+"="+fi.GetValue(f)+" ");}
UnityEngine.Object.DestroyImmediate(go);
return sb.ToString();
