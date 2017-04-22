using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using HexMage.GUI.Components;
using HexMage.GUI.Core;
using HexMage.Simulator;
using HexMage.Simulator.Model;
using HexMage.Simulator.Pathfinding;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json;
using Keys = Microsoft.Xna.Framework.Input.Keys;

namespace HexMage.GUI.Scenes {
    public class MapEditor : Component {
        private readonly Action<Map> _loadNewMap;
        private readonly Func<Map> _mapFunc;

        public MapEditor(Func<Map> mapFunc, Action<Map> loadNewMap) {
            _mapFunc = mapFunc;
            _loadNewMap = loadNewMap;
        }

        public override void Update(GameTime time) {
            base.Update(time);
            var inputManager = InputManager.Instance;

            var camera = Camera2D.Instance;
            var map = _mapFunc();
            var mouseHex = camera.MouseHex;

            camera.NavigationEnabled = false;

            if (inputManager.IsKeyJustPressed(Keys.R) && Keyboard.GetState().IsKeyDown(Keys.LeftControl)) {
                SceneManager.RollbackToFirst = true;
                return;
            }

            if (inputManager.IsKeyJustPressed(Keys.L)) {
                var fileDialog = new OpenFileDialog {CheckFileExists = true};
                if (fileDialog.ShowDialog() == DialogResult.OK) {
                    Utils.Log(LogSeverity.Info, nameof(MapEditor), $"Loaded file {fileDialog.FileName}");


                    using (var stream = fileDialog.OpenFile()) {
                        _loadNewMap(LoadMapFromStream(stream));
                    }
                }
            }

            if (inputManager.IsKeyJustPressed(Keys.S)) {
                var fileDialog = new OpenFileDialog {
                    CheckFileExists = false
                };
                try {
                    if (fileDialog.ShowDialog() == DialogResult.OK) {
                        Utils.Log(LogSeverity.Info, nameof(MapEditor), $"Saved to file {fileDialog.FileName}");

                        using (var writer = new StreamWriter(fileDialog.FileName)) {
                            var data = JsonConvert.SerializeObject(map);
                            writer.Write(data);
                            //new MapRepresentation(map).SaveToStream(writer);
                        }
                    }
                } catch (IOException e) {
                    MessageBox.Show(e.Message);
                }
            }

            const int MaxStartingPointCount = 3;

            if (map.IsValidCoord(mouseHex)) {
                bool hoverBlue = false;
                bool hoverRed = false;
                if (map.BlueStartingPoints.Contains(mouseHex)) {
                    hoverBlue = true;
                }
                if (map.RedStartingPoints.Contains(mouseHex)) {
                    hoverRed = true;
                }

                if (hoverRed || hoverBlue) {
                    List<AxialCoord> hoverList = hoverRed ? map.RedStartingPoints : map.BlueStartingPoints;

                    ReorderStartingPoint(Keys.D1, 0, mouseHex, hoverList);
                    ReorderStartingPoint(Keys.D2, 1, mouseHex, hoverList);
                    ReorderStartingPoint(Keys.D3, 2, mouseHex, hoverList);
                }

                if (inputManager.JustRightClickReleased()
                    || inputManager.IsKeyJustPressed(Keys.W)) {
                    if (map[mouseHex] == HexType.Empty) {
                        map.BlueStartingPoints.Remove(mouseHex);
                        map.RedStartingPoints.Remove(mouseHex);

                        map[mouseHex] = HexType.Wall;
                    } else {
                        map[mouseHex] = HexType.Empty;
                    }
                }


                List<AxialCoord> togglePoints = null;
                if (inputManager.IsKeyJustPressed(Keys.Q)) {
                    togglePoints = map.RedStartingPoints;
                } else if (inputManager.IsKeyJustPressed(Keys.E)) {
                    togglePoints = map.BlueStartingPoints;
                }

                if (togglePoints != null) {
                    if (map[mouseHex] == HexType.Wall) {
                        map[mouseHex] = HexType.Empty;
                    }

                    if (togglePoints.Contains(mouseHex)) {
                        togglePoints.Remove(mouseHex);
                    } else {
                        map.BlueStartingPoints.Remove(mouseHex);
                        map.RedStartingPoints.Remove(mouseHex);

                        if (togglePoints.Count >= MaxStartingPointCount) {
                            togglePoints.RemoveAt(0);
                        }
                        togglePoints.Add(mouseHex);
                    }
                }
            }
        }

        public static Map LoadMapFromStream(Stream stream) {
            using (var reader = new StreamReader(stream)) {
                var content = reader.ReadToEnd();
                return JsonConvert.DeserializeObject<Map>(content);
            }
        }

        public static Map LoadMapFromFile(string filename) {
            using (var reader = new StreamReader(filename)) {
                var content = reader.ReadToEnd();
                return JsonConvert.DeserializeObject<Map>(content);
            }
        }

        private void ReorderStartingPoint(Keys key, int index, AxialCoord mouseHex, List<AxialCoord> hoverList) {
            if (InputManager.Instance.IsKeyJustPressed(key)) {
                var tmp = hoverList[index];
                var i = hoverList.IndexOf(mouseHex);
                hoverList[index] = hoverList[i];
                hoverList[i] = tmp;
            }
        }
    }
}