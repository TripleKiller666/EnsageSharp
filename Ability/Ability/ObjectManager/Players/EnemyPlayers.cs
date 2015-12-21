﻿namespace Ability.ObjectManager.Players
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Ability.OnUpdate;

    using Ensage;
    using Ensage.Common;
    using Ensage.Common.Extensions;

    internal class EnemyPlayers
    {
        #region Static Fields

        public static List<Player> All;

        #endregion

        #region Public Methods and Operators

        public static void Update(EventArgs args)
        {
            if (!OnUpdateChecks.CanUpdate())
            {
                return;
            }
            if (!Utils.SleepCheck("Players.Update"))
            {
                return;
            }
            if (All.Count < 5)
            {
                All = ObjectMgr.GetEntities<Player>().Where(x => x.Team == AbilityMain.Me.GetEnemyTeam()).ToList();
            }
            Utils.Sleep(1000, "Players.Update");
        }

        #endregion
    }
}