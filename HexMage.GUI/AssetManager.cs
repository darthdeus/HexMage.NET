using System;
using System.Collections.Generic;
using System.Diagnostics;
using HexMage.Simulator;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace HexMage.GUI {
    public class AssetManager {
        public static readonly string DarkMageIdle = "mobs/dark-mage-idle";
        public static readonly string DarkMageClicked = "mobs/dark-mage-clicked";
        public static readonly string MobSprite = "mobs/mage";

        public static readonly string FireballSprite = "mobs/fireball";
        public static readonly string FireballExplosionSprite = "mobs/fireball-explosion";
        public static readonly string FrostboltSprite = "mobs/frostbolt";
        public static readonly string FrostboltExplosionSprite = "mobs/frostbolt-explosion";
        public static readonly string EarthboltSprite = "mobs/earthbolt";
        public static readonly string EarthboltExplosionSprite = "mobs/earthbolt-explosion";
        public static readonly string LightningSprite = "mobs/lightning";
        public static readonly string LightningExplosionSprite = "mobs/lightning-explosion";

        public static readonly string HexWallSprite = "tiles/wall_hex";
        public static readonly string HexPathSprite = "tiles/path_hex";
        public static readonly string HexEmptySprite = "tiles/photoshopTile";
        public static readonly string HexGraySprite = "tiles/gray";
        public static readonly string HexHoverSprite = "tiles/hover_hex";
        public static readonly string HexTargetSprite = "tiles/target_hex";
        public static readonly string HexWithinDistance = "tiles/hex_within_distance";

        public static readonly string NoTexture = "ability_ui/magenta";
        public static readonly string SpellHighlight = "ability_ui/spell_highlight";

        public static readonly string SpellEarthBG = "ability_ui/spell_earth_bg";
        public static readonly string SpellEarthActiveBG = "ability_ui/spell_earth_active_bg";
        public static readonly string SpellFireBG = "ability_ui/spell_fire_bg";
        public static readonly string SpellFireActiveBG = "ability_ui/spell_fire_active_bg";
        public static readonly string SpellWaterBG = "ability_ui/spell_water_bg";
        public static readonly string SpellWaterActiveBG = "ability_ui/spell_water_active_bg";
        public static readonly string SpellAirBG = "ability_ui/spell_air_bg";
        public static readonly string SpellAirActiveBG = "ability_ui/spell_air_active_bg";

        public static readonly string ShaderAbility = "shaders/ability_shader";

        public static readonly string ParticleSprite = "particle";

        private static readonly string FontName = "Arial";

        private readonly ContentManager _contentManager;
        private readonly Dictionary<string, Texture2D> _textures = new Dictionary<string, Texture2D>();
        private readonly Dictionary<string, Effect> _effects = new Dictionary<string, Effect>();
        private SpriteFont _font;

        public AssetManager(ContentManager contentManager) {
            _contentManager = contentManager;
        }

        public Texture2D this[string name] {
            get {
                if (_textures.ContainsKey(name)) {
                    return _textures[name];
                } else {
                    _textures[name] = _contentManager.Load<Texture2D>(name);
                    return _textures[name];
                }
            }
        }

        public Effect LoadEffect(string name) {
            if (_effects.ContainsKey(name)) {
                return _effects[name];
            } else {
                _effects[name] = _contentManager.Load<Effect>(name);
                return _effects[name];
            }
        }

        public SpriteFont Font {
            get {
                Debug.Assert(_font != null);
                return _font;
            }
        }


        public void Preload() {
            _font = _contentManager.Load<SpriteFont>(FontName);
            foreach (var texture in new[] {HexWallSprite, HexPathSprite, MobSprite, HexEmptySprite}) {
                _textures[texture] = _contentManager.Load<Texture2D>(texture);
            }
        }

        public void RegisterTexture(string name, Texture2D texture2D) {
            _textures[name] = texture2D;
        }

        public static string ProjectileSpriteForElement(AbilityElement element) {
            switch (element) {
                case AbilityElement.Earth:
                    return EarthboltSprite;
                case AbilityElement.Fire:
                    return FireballSprite;
                case AbilityElement.Air:
                    return LightningSprite;
                case AbilityElement.Water:
                    return FrostboltSprite;
            }

            throw new ArgumentException($"Invalid element type {element}", nameof(element));
        }


        public static string ProjectileExplosionSpriteForElement(AbilityElement element) {
            switch (element) {
                case AbilityElement.Earth:
                    return EarthboltExplosionSprite;
                case AbilityElement.Fire:
                    return FireballExplosionSprite;
                case AbilityElement.Air:
                    return LightningExplosionSprite;
                case AbilityElement.Water:
                    return FrostboltExplosionSprite;
            }

            throw new ArgumentException($"Invalid element type {element}", nameof(element));
        }
    }
}