using System;
using System.Collections.Generic;
using System.Globalization;
using BusJam.Core;
using BusJam.Gameplay;
using BusJam.Level;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BusJam.LevelEditor
{
    /// <summary>Play modunda taslak üzerinde çalışır. Yalnız geçerli taslak kayıtta asset'e aktarılır.</summary>
    public class LevelEditorPanel : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private LevelDatabase database;
        [SerializeField] private BoardView boardView;
        [SerializeField] private Camera boardCamera;
        [SerializeField] private TMP_Dropdown levelList, brush, color, facing;
        [SerializeField] private UnityEngine.UI.Toggle hidden;
        [SerializeField] private TMP_InputField width, height, seconds, buses, tunnelQueue;
        [SerializeField] private TMP_Text status;

        private readonly Board board = new Board();
        private LevelData source;
        private LevelData draft;

        private void Start()
        {
            SetOptions(brush, Enum.GetNames(typeof(CellType)));
            SetOptions(color, Enum.GetNames(typeof(PassengerColor)));
            SetOptions(facing, Enum.GetNames(typeof(Direction)));
            RefreshLevelList();
            if (database.Count > 0)
                LoadSelected();
            else
                NewLevel();
        }

        private static void SetOptions(TMP_Dropdown dropdown, string[] names)
        {
            dropdown.ClearOptions();
            dropdown.AddOptions(new List<string>(names));
        }

        private void RefreshLevelList()
        {
            var names = new string[database.Count];
            for (int i = 0; i < names.Length; i++)
                names[i] = database.Get(i).name;
            SetOptions(levelList, names);
        }

        public void NewLevel() => Open(null);

        public void LoadSelected()
        {
            if (database.Count > 0)
                Open(database.Get(levelList.value));
        }

        private void Open(LevelData asset)
        {
            if (draft != null)
                Destroy(draft);
            source = asset;
            draft = asset == null ? ScriptableObject.CreateInstance<LevelData>() : Instantiate(asset);
            width.text = draft.width.ToString();
            height.text = draft.height.ToString();
            seconds.text = draft.timeLimit.ToString(CultureInfo.CurrentCulture);
            buses.text = string.Join(", ", draft.busOrder);
            status.text = asset == null ? "Yeni level" : asset.name;
            Preview();
        }

        private void OnDestroy()
        {
            if (draft != null)
                Destroy(draft);
        }

        private void Preview()
        {
            board.Setup(draft);
            boardView.Build(draft, board);
        }

        public void ResizeGrid()
        {
            if (ReadSize(out int w, out int h))
            {
                draft.Resize(w, h);
                Preview();
                status.text = $"{w} × {h}";
            }
        }

        private bool ReadSize(out int w, out int h)
        {
            int.TryParse(width.text, out w);
            int.TryParse(height.text, out h);
            if (w >= 1 && w <= GameRules.MaxWidth && h >= 1 && h <= GameRules.MaxHeight)
                return true;
            status.text = $"Boyut 1–{GameRules.MaxWidth} × 1–{GameRules.MaxHeight} olmalı.";
            return false;
        }

        // Şeffaf tıklama alanı gridin üzerindedir; kontrol paneli tıklamaları kendi üzerinde tutar.
        public void OnPointerClick(PointerEventData pointer)
        {
            if (draft == null || !boardView.TryPickCell(boardCamera.ScreenPointToRay(pointer.position), out var cell)
                || !board.InBounds(cell))
                return;

            var type = (CellType)brush.value;
            TunnelData tunnel = null;
            if (type == CellType.Tunnel)
            {
                if (!ReadColors(tunnelQueue.text, out var queue))
                    return;
                tunnel = new TunnelData { cell = cell, facing = (Direction)facing.value, queue = queue };
            }

            draft.tunnels.RemoveAll(item => item.cell == cell);
            if (tunnel != null)
                draft.tunnels.Add(tunnel);
            draft.SetCell(cell.x, cell.y, new CellData
            {
                type = type, color = (PassengerColor)color.value, hidden = type == CellType.Passenger && hidden.isOn
            });
            Preview();
            status.text = $"({cell.x}, {cell.y}) {type}";
        }

        private bool ReadColors(string text, out List<PassengerColor> colors)
        {
            colors = new List<PassengerColor>();
            if (string.IsNullOrWhiteSpace(text))
                return true;
            foreach (string token in text.Split(','))
            {
                string name = token.Trim();
                if (!Enum.TryParse(name, true, out PassengerColor parsed) ||
                    !string.Equals(name, parsed.ToString(), StringComparison.OrdinalIgnoreCase) ||
                    !Enum.IsDefined(typeof(PassengerColor), parsed))
                {
                    status.text = $"Geçersiz renk: {name}. Renk adlarını virgülle ayır.";
                    return false;
                }
                colors.Add(parsed);
            }
            return true;
        }

        public void SaveLevel()
        {
#if UNITY_EDITOR
            if (!ReadSize(out int w, out int h))
                return;
            if (w != draft.width || h != draft.height)
            {
                status.text = "Önce boyutu uygula.";
                return;
            }
            if (!float.TryParse(seconds.text, out float duration) || !(duration >= 1f) || float.IsInfinity(duration))
            {
                status.text = "Süre en az 1 olmalı.";
                return;
            }
            if (!ReadColors(buses.text, out var order))
                return;
            draft.timeLimit = duration;
            draft.busOrder = order;
            if (!draft.Validate(out string error))
            {
                status.text = error;
                return;
            }

            if (source == null)
            {
                string path = UnityEditor.AssetDatabase.GenerateUniqueAssetPath(
                    $"Assets/_BusJam/Data/Levels/Level_{database.Count + 1:00}.asset");
                var asset = Instantiate(draft);
                asset.name = System.IO.Path.GetFileNameWithoutExtension(path);
                UnityEditor.AssetDatabase.CreateAsset(asset, path);
                source = asset;
                database.Add(source);
                UnityEditor.EditorUtility.SetDirty(database);
                RefreshLevelList();
                levelList.value = database.Count - 1;
            }
            else
            {
                draft.name = source.name;
                UnityEditor.EditorUtility.CopySerialized(draft, source);
                UnityEditor.EditorUtility.SetDirty(source);
            }
            UnityEditor.AssetDatabase.SaveAssets();
            status.text = $"{source.name} kaydedildi.";
#else
            status.text = "Asset kaydı Unity Editor içinde kullanılabilir.";
#endif
        }
    }
}
