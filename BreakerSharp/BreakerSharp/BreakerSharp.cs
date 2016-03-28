﻿namespace BreakerSharp
{
    using System.Reflection;

    using Ensage;
    using Ensage.Common;
    using Ensage.Common.Extensions;
    using Ensage.Common.Objects;

    using global::BreakerSharp.Abilities;
    using global::BreakerSharp.Utilities;

    /// <summary>
    ///     The breaker sharp.
    /// </summary>
    public class BreakerSharp
    {
        #region Fields

        /// <summary>
        ///     The combo sleeper.
        /// </summary>
        private readonly Sleeper comboSleeper;

        /// <summary>
        ///     The item combo.
        /// </summary>
        private readonly ItemCombo itemCombo;

        /// <summary>
        ///     The greater bash.
        /// </summary>
        private GreaterBash greaterBash;

        /// <summary>
        ///     The move.
        /// </summary>
        private Move move;

        /// <summary>
        ///     The pause.
        /// </summary>
        private bool pause;

        /// <summary>
        ///     The target find.
        /// </summary>
        private TargetFind targetFind;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///     Initializes a new instance of the <see cref="BreakerSharp" /> class.
        /// </summary>
        public BreakerSharp()
        {
            this.comboSleeper = new Sleeper();
            this.itemCombo = new ItemCombo();
        }

        #endregion

        #region Properties

        /// <summary>
        ///     Gets the me.
        /// </summary>
        private static Hero Me
        {
            get
            {
                return Variables.Hero;
            }
        }

        /// <summary>
        ///     Gets the target.
        /// </summary>
        private Hero Target
        {
            get
            {
                return this.targetFind.Target;
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        ///     The on close.
        /// </summary>
        public void OnClose()
        {
            this.pause = true;
            if (this.itemCombo != null)
            {
                this.itemCombo.Pause = true;
            }

            if (Variables.MenuManager != null)
            {
                Variables.MenuManager.Menu.RemoveFromMainMenu();
            }
        }

        /// <summary>
        ///     The on draw.
        /// </summary>
        public void OnDraw()
        {
            if (this.pause || Variables.Hero == null || !Variables.Hero.IsValid)
            {
                return;
            }

            if (Variables.MenuManager.DrawTarget)
            {
                this.targetFind.DrawTarget();
            }

            if (Variables.MenuManager.DrawChance)
            {
                this.greaterBash.CheckProc();
                this.greaterBash.DrawChance();
            }

            if (Variables.MenuManager.DrawTimeToHit)
            {
                Variables.ChargeOfDarkness.DrawTimeToHit(this.Target);
            }
        }

        /// <summary>
        ///     The on load.
        /// </summary>
        public void OnLoad()
        {
            Variables.Hero = ObjectManager.LocalHero;
            this.pause = Variables.Hero.ClassID != ClassID.CDOTA_Unit_Hero_SpiritBreaker;
            this.itemCombo.Pause = this.pause;
            if (this.pause)
            {
                return;
            }

            Variables.Hero = ObjectManager.LocalHero;
            Variables.EnemyTeam = Me.GetEnemyTeam();
            Variables.MenuManager = new MenuManager(Me.Name);
            Variables.ChargeOfDarkness = new ChargeOfDarkness(Me.Spellbook.Spell1);
            Variables.NetherStrike = new NetherStrike(Variables.Hero.Spellbook.Spell4);
            Variables.MenuManager.Menu.AddToMainMenu();

            this.greaterBash = new GreaterBash(Me.Spellbook.Spell3);
            this.targetFind = new TargetFind();
            this.move = new Move(Me);
            this.itemCombo.UpdateItems();

            Game.PrintMessage(
                Constants.AssemblyName + " v" + Assembly.GetExecutingAssembly().GetName().Version + " loaded", 
                MessageType.LogMessage);
        }

        /// <summary>
        ///     The on update.
        /// </summary>
        public void OnUpdate()
        {
            if (!this.pause)
            {
                this.pause = Game.IsPaused;
            }

            if (this.pause || Variables.Hero == null || !Variables.Hero.IsValid)
            {
                return;
            }

            var canAutoUse = !Me.IsInvisible() && !Me.IsChanneling();
            if (Variables.ArmletToggler != null && Variables.ArmletToggler.CanToggle && canAutoUse)
            {
                Variables.ArmletToggler.Toggle();
                this.comboSleeper.Sleep(Game.Ping + 100);
                return;
            }

            if (Variables.ChargeAway)
            {
                Variables.ChargeOfDarkness.ChargeAway();
                return;
            }

            this.targetFind.Find();

            if (Variables.Combo)
            {
                if (this.comboSleeper.Sleeping)
                {
                    return;
                }

                if (Variables.ChargeOfDarkness.IsCharging)
                {
                    this.itemCombo.UseInvis(false);
                    return;
                }

                if (this.Target == null || !this.Target.IsVisible || !this.Target.IsAlive)
                {
                    this.move.ToPosition(Game.MousePosition);
                    return;
                }

                var canDoCombo = Orbwalking.CanCancelAnimation() && Utils.SleepCheck("Orbwalk.Attack")
                                 && Utils.SleepCheck("GlobalCasting")
                                 && (this.Target.Health
                                     > this.Target.DamageTaken(Me.DamageAverage, DamageType.Physical, Me) * 2
                                     || this.Target.Distance2D(Me) > Me.GetAttackRange() + 100);

                if (Variables.MenuManager.AbilityEnabled("spirit_breaker_charge_of_darkness") && canDoCombo
                    && Variables.ChargeOfDarkness.ChargeTo(this.Target))
                {
                    this.itemCombo.UseInvis(true);
                    this.comboSleeper.Sleep((float)((Variables.ChargeOfDarkness.CastPoint * 1000) + Game.Ping + 200));
                    return;
                }

                if (canDoCombo && this.itemCombo.ExecuteCombo(this.Target))
                {
                    this.comboSleeper.Sleep((float)(Game.Ping + Me.GetTurnTime(this.Target.Position)));
                    return;
                }

                if (Variables.MenuManager.AbilityEnabled("spirit_breaker_nether_strike") && canDoCombo
                    && this.Target.Health > Variables.MenuManager.MinHpKillsteal
                    && Variables.NetherStrike.UseOn(this.Target))
                {
                    this.comboSleeper.Sleep((float)(Variables.NetherStrike.CastPoint * 1000));
                    return;
                }

                Orbwalking.Orbwalk(this.Target);
                return;
            }

            if (canAutoUse && Variables.MenuManager.KillSteal
                && Variables.NetherStrike.KillSteal(Variables.MenuManager.MinHpKillsteal))
            {
                this.comboSleeper.Sleep((float)(Game.Ping + (Variables.NetherStrike.CastPoint * 1000)));
            }
        }

        /// <summary>
        ///     The on wnd proc.
        /// </summary>
        /// <param name="args">
        ///     The args.
        /// </param>
        public void OnWndProc(WndEventArgs args)
        {
            if (this.pause || Variables.Hero == null || !Variables.Hero.IsValid)
            {
                return;
            }

            if (args.Msg != (ulong)Utils.WindowsMessages.WM_LBUTTONDOWN)
            {
                return;
            }

            if (this.Target == null || !this.Target.IsValid)
            {
                return;
            }

            if (this.Target.Distance2D(Game.MousePosition) < 200)
            {
                this.targetFind.LockTarget();
            }
            else
            {
                this.targetFind.UnlockTarget();
                this.targetFind.Find();
                if (this.Target.Distance2D(Game.MousePosition) < 200)
                {
                    this.targetFind.LockTarget();
                }
            }
        }

        /// <summary>
        ///     The player_ on execute order.
        /// </summary>
        /// <param name="args">
        ///     The args.
        /// </param>
        public void Player_OnExecuteOrder(ExecuteOrderEventArgs args)
        {
            if (this.pause || Variables.Hero == null || !Variables.Hero.IsValid)
            {
                return;
            }

            if (Variables.ArmletToggler != null && args.Order == Order.ToggleAbility && args.Ability != null
                && args.Ability.StoredName() == "item_armlet")
            {
                Variables.ArmletToggler.Sleeper.Sleep(Variables.MenuManager.ArmletToggleInterval / 2);
            }

            if (args.Ability != null && args.Target != null
                && args.Ability.StoredName() == "spirit_breaker_charge_of_darkness")
            {
                var target = args.Target as Unit;
                if (target != null)
                {
                    Variables.ChargeOfDarkness.ChargeTo(target);
                }
            }
        }

        #endregion
    }
}
