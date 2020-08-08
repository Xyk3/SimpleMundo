using System;
using System.Drawing;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using EnsoulSharp.SDK.MenuUI.Values;
using EnsoulSharp.SDK.Prediction;
using EnsoulSharp.SDK.Utility;

namespace SimpleMundo
{
    public static class Program
    {
        private static Menu _mainMenu;

        private static Spell _q;
        private static Spell _w;
        private static Spell _e;
        private static Spell _r;

        private static readonly int[] WCost = {0, 10, 15, 20, 25, 30};

        private static readonly AIHeroClient Player = ObjectManager.Player;

        private static void Main(string[] args)
        {
            GameEvent.OnGameLoad += OnGameLoad;
        }

        private static void OnGameLoad()
        {
            if (ObjectManager.Player.CharacterName != "DrMundo") return;
            Game.Print(
                "<font size='28' color='#4CAF50'>SimpleMundo</font><font size='26' color='#03A9F4'>" +
                "successfully loaded!</font><br><font size='26' color='#9E9E9E'>Made by ٴٴٴٴ</font><br>");

            _q = new Spell(SpellSlot.Q, 975f);
            _q.SetSkillshot(0.25f, 60f, 1850, true, false, SkillshotType.Line);

            _w = new Spell(SpellSlot.W, 325f);

            _e = new Spell(SpellSlot.E, 125f);

            _r = new Spell(SpellSlot.R, 125f);

            _mainMenu = new Menu("SimpleMundo", "SimpleMundo by ٴٴٴٴ", true);

            var comboMenu = new Menu("Combo", "Combo Settings")
            {
                new MenuBool("comboQ", "Use Q"),
                new MenuBool("comboW", "Use W"),
                new MenuBool("comboE", "Use E"),
                new MenuBool("comboR", "Use R"),
                new MenuSlider("healthR", "Use R when at x% Health", 70)
            };
            _mainMenu.Add(comboMenu);

            var harassMenu = new Menu("Harass", "Harass Settings")
            {
                new MenuBool("harassQ", "Use Q"),
                new MenuBool("harassW", "Use W"),
                new MenuBool("harassE", "Use E")
            };
            _mainMenu.Add(harassMenu);

            var lastHitMenu = new Menu("LastHit", "LastHit Settings")
            {
                new MenuBool("LastHitQ", "Use Q to LastHit")
            };
            _mainMenu.Add(lastHitMenu);

            var clearMenu = new Menu("Clear", "Clear Settings")
            {
                new MenuBool("clearQ", "Use Q"),
                new MenuBool("clearW", "Use W"),
                new MenuBool("clearE", "Use E")
            };
            _mainMenu.Add(clearMenu);


            var miscMenu = new Menu("Misc", "Misc Settings")
            {
                new MenuBool("autoQ", "Auto Q if enemy champ is in range"),
                new MenuBool("aaCancelE", "Try to AA cancel with E"),
                new MenuBool("alwaysW", "Keep W active if enough HP regen")
            };
            _mainMenu.Add(miscMenu);

            var drawMenu = new Menu("Draw", "Draw Settings")
            {
                new MenuBool("drawQ", "Draw Q Range")
            };
            _mainMenu.Add(drawMenu);

            _mainMenu.Add(new MenuSeparator("credits", "Made by ٴٴٴٴ"));

            _mainMenu.Attach();

            Game.OnUpdate += OnUpdate;
            Drawing.OnDraw += OnDraw;
        }

        private static void ThrowQ()
        {
            var target = TargetSelector.GetTarget(_q.Range);
            if (target == null || !target.IsValidTarget(_q.Range)) return;
            var pred = _q.GetPrediction(target);
            if (pred.Hitchance >= HitChance.High) _q.Cast(pred.CastPosition);
        }

        private static void Combo()
        {
            if (_mainMenu["Combo"]["comboQ"].GetValue<MenuBool>().Enabled && _q.IsReady()) ThrowQ();
            if (_mainMenu["Combo"]["comboW"].GetValue<MenuBool>().Enabled && _w.IsReady() && _w.ToggleState == 1 &&
                Player.CountEnemyHeroesInRange(_w.Range) >= 1) _w.Cast();
            if (_mainMenu["Combo"]["comboE"].GetValue<MenuBool>().Enabled && _e.IsReady() &&
                Player.CountEnemyHeroesInRange(Player.AttackRange + 25) >= 1) _e.Cast();
            if (_mainMenu["Combo"]["comboR"].GetValue<MenuBool>().Enabled && _r.IsReady() && Player.HealthPercent <=
                _mainMenu["Combo"]["healthR"].GetValue<MenuSlider>().Value) _r.Cast();
        }

        private static void Clear()
        {
            var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(_q.Range) && x.IsMinion());
            var monster = GameObjects.Jungle.Where(x => x.IsValidTarget(_q.Range))
                .FirstOrDefault(x => x.IsValidTarget(_q.Range));

            var useQ = _mainMenu["Clear"]["clearQ"].GetValue<MenuBool>().Enabled;
            var useE = _mainMenu["Clear"]["clearW"].GetValue<MenuBool>().Enabled;
            var useW = _mainMenu["Clear"]["clearE"].GetValue<MenuBool>().Enabled;

            // lane
            foreach (var minion in minions)
            {
                if (useQ && _q.IsReady()) _q.Cast(minion);
                if (useW && _w.IsReady() && _w.ToggleState == 1 && minion.IsValidTarget(_w.Range)) _w.Cast();

                if (useE && _e.IsReady() && minion.IsValidTarget(_e.Range)) _e.Cast();
            }

            if (monster == null) return;
            // jungle
            if (useQ && _q.IsReady() && monster.IsValidTarget(_q.Range)) _q.Cast(monster);
            if (useW && _w.IsReady() && _w.ToggleState == 1 && monster.IsValidTarget(_w.Range)) _w.Cast();
            if (useE && _e.IsReady() && monster.IsValidTarget(_e.Range)) _e.Cast();
        }

        private static void Harass()
        {
            if (_mainMenu["Harass"]["harassQ"].GetValue<MenuBool>().Enabled && _q.IsReady()) ThrowQ();
            if (_mainMenu["Harass"]["harassW"].GetValue<MenuBool>().Enabled && _w.IsReady() && _w.ToggleState == 1 &&
                Player.CountEnemyHeroesInRange(_w.Range) >= 1) _w.Cast();
            if (_mainMenu["Harass"]["harassE"].GetValue<MenuBool>().Enabled && _e.IsReady() &&
                Player.CountEnemyHeroesInRange(Player.AttackRange + 25) >= 1) _e.Cast();
        }

        private static void LastHit()
        {
            var minions = GameObjects.EnemyMinions.Where(e => e.IsValidTarget(_q.Range));
            foreach (var minion in minions)
            {
                if (!_mainMenu["LastHit"]["LastHitQ"].GetValue<MenuBool>().Enabled || !_q.IsReady()) continue;
                if (QDamage(minion) >= minion.Health + minion.AllShield)
                    _q.Cast(minion);
            }
        }

        private static void OnUpdate(EventArgs args)
        {
            switch (Orbwalker.ActiveMode)
            {
                case OrbwalkerMode.Combo:
                    Combo();
                    break;
                case OrbwalkerMode.LaneClear:
                    Clear();
                    break;
                case OrbwalkerMode.Harass:
                    Harass();
                    break;
                case OrbwalkerMode.LastHit:
                    LastHit();
                    break;
                case OrbwalkerMode.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (_mainMenu["Misc"]["autoQ"].GetValue<MenuBool>().Enabled) AutoQ();
            if (_mainMenu["Misc"]["alwaysW"].GetValue<MenuBool>().Enabled) AlwaysW();
        }

        private static void AutoQ()
        {
            if (_q.IsReady()) ThrowQ();
        }

        private static void AlwaysW()
        {
            if (Player.HPRegenRate >= WCost[_w.Level] && _w.ToggleState == 1 && _w.IsReady()) _w.Cast();
            else if (Player.HPRegenRate < WCost[_w.Level] && _w.ToggleState == 2 && _w.IsReady() &&
                     NobodyAroundW()) _w.Cast();
        }

        private static bool NobodyAroundW()
        {
            var enemy = GameObjects.Enemy.Count(x => x.IsValidTarget(_w.Range));
            var monster = GameObjects.Jungle.Count(x => x.IsValidTarget(_w.Range));
            return enemy == 0 || monster == 0;
        }

        private static double QDamage(AIBaseClient target)
        {
            var qLevel = _q.Level;
            var qPercentage = new[] {0, 20, 22.5f, 25, 27.5f, 30}[qLevel] / 100 + 1;
            var minQDamage = new[] {0, 80, 130, 180, 230, 280}[qLevel];
            return qPercentage * target.Health < minQDamage
                ? Player.CalculateDamage(target, DamageType.Magical,
                    qPercentage * target.Health)
                : Player.CalculateDamage(target, DamageType.Magical, minQDamage);
        }

        private static void OnDraw(EventArgs args)
        {
            if (Player.IsDead) return;

            if (_mainMenu["Draw"]["drawQ"].GetValue<MenuBool>().Enabled)
                Render.Circle.DrawCircle(Player.Position, _q.Range, Color.DarkRed);
        }
    }
}