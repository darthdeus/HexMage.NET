using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HexMage.GUI.Core;
using HexMage.GUI.Renderers;
using HexMage.Simulator;
using HexMage.Simulator.AI;
using HexMage.Simulator.Model;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;

namespace HexMage.GUI.UI {
    public class HistoryLogEntry : Entity, IRenderer {
        private readonly int _actionIndex;
        private readonly AssetManager _assetManager;
        public TeamColor CurrentTeam;
        public string Message;

        public HistoryLogEntry(int actionIndex, TeamColor currentTeam, UctAction action, CachedMob mob, CachedMob target,
                               AbilityInfo abilityInfo,
                               int? moveCost,
                               AssetManager assetManager) {
            _actionIndex = actionIndex;
            _assetManager = assetManager;
            Renderer = this;
            CurrentTeam = currentTeam;

            Message = BuildMessage(action, mob, target, abilityInfo, moveCost);
        }

        protected override void Layout() {
            var font = _assetManager.Font;
            var height = font.MeasureString(Message).Y;

            // TODO - calculate the width properly
            LayoutSize = new Vector2(650, height);
        }

        public string BuildMessage(UctAction action, CachedMob mob, CachedMob target, AbilityInfo abilityInfo, int? moveCost) {
            if (action.Type == UctActionType.Null) {
                return " ";
            }

            string str;
            switch (action.Type) {
                case UctActionType.AbilityUse:
                    str = $"Did {abilityInfo.Dmg} damage for {abilityInfo.Cost} AP.";
                    break;
                case UctActionType.AttackMove:
                    str = $"Moved towards enemy for {moveCost} AP and did {abilityInfo.Dmg} damage for {abilityInfo.Cost} AP.";                    
                    break;
                case UctActionType.Move:
                    str = $"Moved from {mob.MobInstance.Coord} to {action.Coord} for {moveCost} AP.";
                    break;
                case UctActionType.DefensiveMove:
                    str = $"Is trying to hide at {action.Coord} for {moveCost} AP.";
                    break; 
                case UctActionType.EndTurn:
                    throw new InvalidOperationException("End turn shouldn't be logged.");
                case UctActionType.Null:
                    throw new InvalidOperationException("Null action should never be logged.");
                default:
                    throw new ArgumentException($"Invalid action type ${action.Type}", nameof(action));
            }

            return $"{string.Format("{0,3}", _actionIndex)}. {str}";
        }

        public void Render(Entity entity, SpriteBatch batch, AssetManager assetManager) {
            var color = CurrentTeam == TeamColor.Red ? Color.Pink : Color.LightBlue;

            var font = assetManager.Font;
            var pos = RenderPosition;

            batch.DrawString(font, Message, pos, color);

            //string logLevelMsg = $"[{LogLevel}]";
            //string tidMsg = $"[TID#{ThreadId}]";
            //string ownerMsg = $"[{Owner}]:";

            //const float separatorSize = 3;

            //float messageHeight = font.MeasureString(Message).Y;

            //float levelWidth = font.MeasureString(logLevelMsg).X;
            //float tidWidth = font.MeasureString(tidMsg).X;
            //float ownerWidth = font.MeasureString(ownerMsg).X;

            //batch.DrawString(font, logLevelMsg, pos, LogLevelColor(LogLevel));
            //var tidColor = Utils.MainThreadId == ThreadId ? Color.Yellow : Color.Red;
            //batch.DrawString(font, tidMsg, pos + new Vector2(levelWidth + separatorSize, 0), tidColor);
            //batch.DrawString(font, $"[{Owner}]",
            //                 pos + new Vector2(levelWidth + tidWidth + 2 * separatorSize, 0),
            //                 Color.Pink);
            //batch.DrawString(font, Message,
            //                 pos + new Vector2(levelWidth + tidWidth + ownerWidth + separatorSize, 0),
            //                 Color.White);

            //if (LogLevel == LogSeverity.Error) {
            //    var stacktraceStr = new StackTrace().ToString();

            //    batch.DrawString(font, stacktraceStr, pos + new Vector2(0, messageHeight), Color.White);
            //}
        }
    }
}