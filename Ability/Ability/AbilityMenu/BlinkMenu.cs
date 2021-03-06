﻿namespace Ability.AbilityMenu
{
    using Ability.Drawings;

    using Ensage;
    using Ensage.Common.Menu;

    internal class BlinkMenu
    {
        #region Public Methods and Operators

        public static void Create(Ability blink)
        {
            var menu = new Menu("Blink", "abilityMenuBlink", false, "item_blink", true);
            menu.AddItem(new MenuItem("abilityMenuShowBlinkRange", "Show Range").SetValue(true)).ValueChanged +=
                delegate(object sender, OnValueChangeEventArgs eventArgs)
                    {
                        RangeDrawing.RangeVisible(blink, eventArgs.GetNewValue<bool>());
                    };

            RangeDrawing.AddRange(blink);
            RangeDrawing.RangeVisible(blink, menu.Item("abilityMenuShowBlinkRange").GetValue<bool>());
            MainMenu.OptionsMenu.AddSubMenu(menu);
        }

        #endregion
    }
}