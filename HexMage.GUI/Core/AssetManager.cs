using System;
using System.Collections.Generic;
using System.Diagnostics;
using HexMage.Simulator.Model;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Color = Microsoft.Xna.Framework.Color;

namespace HexMage.GUI.Core {
    /// <summary>
    /// AssetManager handles all asset loading throughout the game. It also provides caching
    /// functionality, as well as the option to preload some of the assets.
    /// </summary>
    public class AssetManager {
        /// <summary>
        /// Size of a single hex tile.
        /// </summary>
        public const int TileSize = 64;
        public const int HalfTileSize = TileSize / 2;

        public const string SoundEffectFireballSmall = "sound_effects/fireball_small";
        public const string SoundEffectFireballLarge = "sound_effects/fireball_large";
        public const string SoundEffectSpellHit = "sound_effects/spell_hit";
        public const string SoundEffectDying = "sound_effects/dying";
        public const string SoundEffectEndTurn = "sound_effects/end_turn";

        public const string SolidGrayColor = "color/gray";

        public const string MapEditorBg = "map_editor_bg";
        public const string TeamSelectionBg = "team_selection_bg";
        public const string QuestionnaireBg = "questionnaire_bg";

        public const string DarkMageIdle = "mobs/dark-mage-idle";
        public const string DarkMageClicked = "mobs/dark-mage-clicked";
        public const string DarkMageDeath = "mobs/dark-mage-death";

        public const string FireballSprite = "mobs/fireball";
        public const string FireballExplosionSprite = "mobs/fireball-explosion";
        public const string FrostboltSprite = "mobs/frostbolt";
        public const string FrostboltExplosionSprite = "mobs/frostbolt-explosion";
        public const string EarthboltSprite = "mobs/earthbolt";
        public const string EarthboltExplosionSprite = "mobs/earthbolt-explosion";
        public const string LightningSprite = "mobs/lightning";
        public const string LightningExplosionSprite = "mobs/lightning-explosion";

        public const string HexWallSprite = "tiles/wall_hex";
        public const string HexPathSprite = "tiles/path_hex";
        public const string HexEmptySprite = "tiles/basic_tile";
        public const string HexHoverSprite = "tiles/hover_hex";
        public const string HexTargetSprite = "tiles/target_hex";
        public const string HexWithinDistance = "tiles/hex_within_distance";

        public const string NoTexture = "ability_ui/magenta";
        public const string SpellHighlight = "ability_ui/spell_highlight";

        public const string SpellBg = "ability_ui/spell_bg";
        public const string SpellEarthBg = "ability_ui/spell_earth_bg";
        public const string SpellEarthActiveBg = "ability_ui/spell_earth_active_bg";
        public const string SpellFireBg = "ability_ui/spell_fire_bg";
        public const string SpellFireActiveBg = "ability_ui/spell_fire_active_bg";
        public const string SpellWaterBg = "ability_ui/spell_water_bg";
        public const string SpellWaterActiveBg = "ability_ui/spell_water_active_bg";
        public const string SpellAirBg = "ability_ui/spell_air_bg";
        public const string SpellAirActiveBg = "ability_ui/spell_air_active_bg";

        public const string SpellBgCooldown = "ability_ui/spell_bg_cooldown";
        public const string SpellBgNotEnoughAp = "ability_ui/spell_bg_not_enough_ap";

        public const string ShaderAbility = "shaders/ability_shader";

        public const string HistoryLogBg = "ability_ui/history_log_bg";
        public const string CrosshairCursor = "ability_ui/crosshair";

        public const string ParticleSprite = "particle";

        private const string FontName = "Arial";
        private const string FontAbility = "AbilityFont";
        private const string FontAbilitySmall = "AbilityFontSmall";

        public const string TitleRedTeam = "ui/title-red-team";
        public const string TitleBlueTeam = "ui/title-blue-team";
        public const string TitleGameFinished = "ui/title-game-finished";
        public const string TitleBg = "ui/title-bg";

        public const string UiLeftBg = "ui/left-bg";
        public const string UiRightBg = "ui/right-bg";

        public const string MobDetailBg = "ability_ui/mob_detail_bg";
        public const string MobDetailBgStone = "ability_ui/mob_detail_bg_stone";
        public const string MobDetailBgEmpty = "ability_ui/mob_detail_bg_empty";

        private readonly ContentManager _contentManager;
        private readonly GraphicsDevice _graphicsDevice;
        private readonly Dictionary<string, Texture2D> _textures = new Dictionary<string, Texture2D>();
        private readonly Dictionary<Color, Texture2D> _colors = new Dictionary<Color, Texture2D>();
        private readonly Dictionary<string, Effect> _effects = new Dictionary<string, Effect>();
        private readonly Dictionary<string, SoundEffect> _soundEffects = new Dictionary<string, SoundEffect>();
        private SpriteFont _font;
        private SpriteFont _abilityFont;
        private SpriteFont _abilityFontSmall;

        public AssetManager(ContentManager contentManager, GraphicsDevice graphicsDevice) {
            _contentManager = contentManager;
            _graphicsDevice = graphicsDevice;
        }

        public Texture2D this[string name] {
            get {
                if (!_textures.ContainsKey(name)) {
                    _textures[name] = _contentManager.Load<Texture2D>(name);
                }
                return _textures[name];
            }
        }

        public Effect LoadEffect(string name) {
            if (!_effects.ContainsKey(name)) {
                _effects[name] = _contentManager.Load<Effect>(name);
            }
            return _effects[name];
        }

        public SoundEffect LoadSoundEffect(string name) {
            if (!_soundEffects.ContainsKey(name)) {
                _soundEffects[name] = _contentManager.Load<SoundEffect>(name);
            }
            return _soundEffects[name];
        }

        public SpriteFont Font
        {
            get
            {
                Debug.Assert(_font != null, "Accessing AssetManager's Font before it's initialized");
                return _font;
            }
        }

        public SpriteFont AbilityFont
        {
            get
            {
                Debug.Assert(_abilityFont != null, "Accessing AssetManager's Ability Font before it's initialized");
                return _abilityFont;
            }
        }

        public SpriteFont AbilityFontSmall
        {
            get
            {
                Debug.Assert(_abilityFontSmall != null, "Accessing AssetManager's Ability Font Small before it's initialized");
                return _abilityFontSmall;
            }
        }


        public Texture2D this[Color color] {
            get {
                if (_colors.ContainsKey(color)) {
                    return _colors[color];
                } else {
                    _colors[color] = TextureGenerator.SolidColor(_graphicsDevice, AssetManager.TileSize,
                                                                 AssetManager.TileSize, color);
                    return _colors[color];
                }
            }
        }

        public void Preload() {
            _font = _contentManager.Load<SpriteFont>(FontName);
            _abilityFont = _contentManager.Load<SpriteFont>(FontAbility);
            _abilityFontSmall = _contentManager.Load<SpriteFont>(FontAbilitySmall);

            foreach (var texture in new[] {HexWallSprite, HexPathSprite, HexEmptySprite}) {
                _textures[texture] = _contentManager.Load<Texture2D>(texture);
            }
        }

        public void RegisterTexture(string name, Texture2D texture2D) {
            _textures[name] = texture2D;
        }
    }
}