namespace EnchantressSharp
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Ensage;
    using Ensage.Common;
    using Ensage.Common.Extensions;
    using Ensage.Common.Menu;
    using Ensage.Items;
    using SharpDX;
    using SharpDX.Direct3D9;

    internal class EnchantressSharp
    {
        private static readonly Menu Menu = new Menu("Enchantress", "Enchantress", true, "npc_dota_hero_enchantress",
            true);

        private static Ability enchant, nature, impetus;
        private static bool Active;
        private static bool CreepActive;
        private static bool dragonLance, hurricanePike, aghs;
        private static Item sheep, shiva, dagon, orchid, pike;
        private static Hero me, target;
        private static float attackdragon, attackdragondraw;
        private static float attackaghs, attackaghsdraw;
        private static float attackdraghs, attackdraghsdraw;
        private static ParticleEffect drawDragon;
        private static ParticleEffect drawAghs;
        private static ParticleEffect drawDraghs;
        private static readonly Dictionary<int, ParticleEffect> DrawDragon = new Dictionary<int, ParticleEffect>();
        private static readonly Dictionary<int, ParticleEffect> DrawAghs = new Dictionary<int, ParticleEffect>();
        private static readonly Dictionary<int, ParticleEffect> DrawDraghs = new Dictionary<int, ParticleEffect>();

        public static void Init()
        {
            Game.OnUpdate += Game_OnUpdate;
            Game.OnUpdate += unitControlled;
            Game.OnWndProc += Game_OnWndProc;
            Drawing.OnDraw += Drawing_OnDraw;

            Menu.AddItem(new MenuItem("enable", "Enable").SetValue(true));
            Menu.AddItem(new MenuItem("comboKey", "Combo Key").SetValue(new KeyBind(32, KeyBindType.Press)));
            Menu.AddItem(new MenuItem("ccontrol", "Creep Control").SetValue(new KeyBind('L', KeyBindType.Toggle)));
            Menu.AddItem(new MenuItem("Skills", "Skills:").SetValue(new AbilityToggler(new Dictionary<string, bool>
            {
                {"enchantress_impetus", true},
                {"enchantress_enchant", true}
            })));
            Menu.AddItem(new MenuItem("Items", "Items:").SetValue(new AbilityToggler(new Dictionary<string, bool>
            {
                {"item_sheepstick", true},
                {"item_shivas_guard", true},
                {"item_dagon", true},
                {"item_orchid", true},
                {"item_hurricane_pike", true},
            })));
            Menu.AddItem(new MenuItem("NatureTog", "Use Nature's Attendants").SetValue(true));
            Menu.AddItem(new MenuItem("NatureAuto", "Min health for auto-nature (%)").SetValue(new Slider(35, 0, 100)));

            var menuDraws = new Menu("Drawings", "draws", false, @"..\other\statpop_clock", true);
            menuDraws.AddItem(new MenuItem("drawattackdragon", "Draw Dragon Lance range").SetValue(false));
            menuDraws.AddItem(new MenuItem("drawattackaghs", "Draw Aghanims range").SetValue(false));
            menuDraws.AddItem(new MenuItem("drawattackdraghs", "Draw Aghanims & Dragon Lance range").SetValue(false));

            Menu.AddToMainMenu();
            Menu.AddSubMenu(menuDraws);
            OnLoadMessage();
        }

        public static void Game_OnUpdate(EventArgs args)
        {
            if (!Game.IsInGame || Game.IsPaused || Game.IsWatchingGame)
                return;

            me = ObjectManager.LocalHero;

            if (me == null || me.ClassID != ClassID.CDOTA_Unit_Hero_Enchantress)
                return;

            if (!Menu.Item("enable").IsActive())
                return;

            target = me.ClosestToMouseTarget(3500);

            if (target == null)
                return;

            enchant = me.Spellbook.SpellW;
            nature = me.Spellbook.SpellE;
            impetus = me.Spellbook.SpellR;
            sheep = me.FindItem("item_sheepstick");
            shiva = me.FindItem("item_shivas_guard");
            dagon = me.Inventory.Items.FirstOrDefault(x => x.Name.Contains("item_dagon"));
            orchid = me.FindItem("item_orchid");
            pike = me.FindItem("item_hurricane_pike");

            aghs = me.HasModifier("modifier_item_ultimate_scepter");
            dragonLance = me.HasModifier("modifier_item_dragon_lance");
            attackdragon = dragonLance ? 670 : 540;
            attackaghs = aghs ? 730 : 540;
            attackdraghs = aghs && dragonLance ? 860 : 540;

            var linkens = target.IsLinkensProtected();
            var ModifRod = target.HasModifier("modifier_item_rod_of_atos_debuff");
            var EnchantModif = target.HasModifier("modifier_enchantress_enchant_slow");

            if (Menu.Item("NatureTog").GetValue<bool>() && me.IsAlive && me.CanCast() && !me.IsChanneling())
            {
                if (nature != null && nature.IsValid && nature.CanBeCasted() &&
                    me.Health <= me.MaximumHealth / 100 * Menu.Item("NatureAuto").GetValue<Slider>().Value &&
                    Utils.SleepCheck("nature"))
                {
                    nature.UseAbility();
                    Utils.Sleep(200, "nature");
                }
            }

            if (Active && me.IsAlive && target.IsAlive && Utils.SleepCheck("activated"))
            {
                var noBlade = target.HasModifier("modifier_item_blade_mail_reflect");
                if (target.IsVisible && me.Distance2D(target) <= 2300 && !noBlade)
                {
                    if (impetus != null && impetus.CanBeCasted() && Menu.Item("Skills").GetValue<AbilityToggler>().IsEnabled(impetus.Name) && Utils.SleepCheck("impetus"))
                    {
                        if (me.CanCast() && me.Distance2D(target) <= attackdraghs)
                        {
                            impetus.UseAbility(target);
                            Utils.Sleep(200 + Game.Ping, "impetus");
                        }
                        else if (me.CanCast() && me.Distance2D(target) <= attackaghs)
                        {
                            impetus.UseAbility(target);
                            Utils.Sleep(200 + Game.Ping, "impetus");
                        }
                        else if (me.CanCast() && me.Distance2D(target) <= attackdragon)
                        {
                            impetus.UseAbility(target);
                            Utils.Sleep(200 + Game.Ping, "impetus");
                        }
                        else if (me.CanCast() && me.Distance2D(target) <= 5000)
                        {
                            impetus.UseAbility(target);
                            Utils.Sleep(200 + Game.Ping, "impetus");
                        }
                        else if (me.CanCast())
                        {
                            impetus.UseAbility(target);
                            Utils.Sleep(200 + Game.Ping, "impetus");
                        }
                    }

                    if (enchant != null && enchant.CanBeCasted() && !ModifRod && me.CanCast() && Menu.Item("Skills").GetValue<AbilityToggler>().IsEnabled(enchant.Name) && Utils.SleepCheck("enchant"))
                    {
                        enchant.UseAbility(target);
                        Utils.Sleep(150 + Game.Ping, "enchant");
                    }

                    if (shiva != null && shiva.CanBeCasted() && me.CanCast() && Menu.Item("Items").GetValue<AbilityToggler>().IsEnabled(shiva.Name) && !target.IsMagicImmune() && Utils.SleepCheck("shiva"))
                    {
                        shiva.UseAbility();
                        Utils.Sleep(250 + Game.Ping, "shiva");
                    }

                    if (sheep != null && sheep.CanBeCasted() && Menu.Item("Items").GetValue<AbilityToggler>().IsEnabled(sheep.Name) && !target.IsMagicImmune() && !linkens && Utils.SleepCheck("sheep"))
                    {
                        if (me.CanCast() && EnchantModif)
                        {
                            sheep.UseAbility(target);
                            Utils.Sleep(250 + Game.Ping, "sheep");
                        }
                        else if (me.CanCast() && !EnchantModif)
                        {
                            sheep.UseAbility(target);
                            Utils.Sleep(250 + Game.Ping, "sheep");
                        }
                    }

                    if (dagon != null && me.CanCast() && !target.IsLinkensProtected() && dagon.CanBeCasted() && Menu.Item("Items").GetValue<AbilityToggler>().IsEnabled("item_dagon") && !target.IsMagicImmune() && Utils.SleepCheck("dagon"))
                    {
                        dagon.UseAbility(target);
                        Utils.Sleep(150 + Game.Ping, "dagon");
                    }

                    if (orchid != null && orchid.CanBeCasted() && me.CanCast() && Menu.Item("Items").GetValue<AbilityToggler>().IsEnabled(orchid.Name) && !target.IsMagicImmune() && !linkens && Utils.SleepCheck("orchid"))
                    {
                        orchid.UseAbility(target);
                        Utils.Sleep(225 + Game.Ping, "orchid");
                    }

                    if (pike != null && pike.CanBeCasted() && me.CanCast() && me.Distance2D(target) <= 500 && Menu.Item("Items").GetValue<AbilityToggler>().IsEnabled(pike.Name) && !target.IsMagicImmune() && Utils.SleepCheck("pike"))
                    {
                        pike.UseAbility(target);
                        Utils.Sleep(200 + Game.Ping, "pike");
                    }
                }
                Utils.Sleep(200, "activated");
            }
        }

        public static void unitControlled(EventArgs args)
        {
            if (!Game.IsInGame || Game.IsPaused || Game.IsWatchingGame)
                return;

            me = ObjectManager.LocalHero;

            if (me == null || me.ClassID != ClassID.CDOTA_Unit_Hero_Enchantress)
                return;

            if (!Menu.Item("enable").IsActive())
                return;

            target = me.ClosestToMouseTarget(3500);

            if (target == null)
                return;

            if (CreepActive && me.IsAlive && Utils.SleepCheck("creepactivated"))
            {
                var Neutral = ObjectManager.GetEntities<Creep>().Where(creep => creep.ClassID == ClassID.CDOTA_BaseNPC_Creep_Neutral && creep.IsAlive && creep.Team == me.Team && creep.IsControllable).ToList();

                for (int i = 0; i < Neutral.Count(); i++)
                {
                    var v = ObjectManager.GetEntities<Hero>().Where(x => x.Team == me.Team && x.IsAlive && x.IsVisible && !x.IsIllusion).ToList();

                    if (Neutral[i].Name == "npc_dota_neutral_ogre_magi")
                    {
                        for (int z = 0; z < v.Count(); z++)
                        {
                            var armor = Neutral[i].Spellbook.SpellQ;

                            if (!v[z].Modifiers.Any(y => y.Name == "modifier_ogre_magi_frost_armor") &&
                                armor.CanBeCasted() && Neutral[i].Position.Distance2D(v[z]) <= 900
                                && Utils.SleepCheck(Neutral[i].Handle.ToString()))
                            {
                                armor.UseAbility(v[z]);
                                Utils.Sleep(400, Neutral[i].Handle.ToString());
                            }
                        }
                    }
                    if (Neutral[i].Name == "npc_dota_neutral_forest_troll_high_priest")
                    {
                        for (int z = 0; z < v.Count(); z++)
                        {
                            if (Neutral[i].Spellbook.SpellQ.CanBeCasted() && Neutral[i].Position.Distance2D(v[z]) <= 900)
                            {
                                if (v[z].Health <= (v[z].MaximumHealth * 0.9)
                                    && Utils.SleepCheck(Neutral[i].Handle.ToString() + "high_priest"))
                                {
                                    Neutral[i].Spellbook.SpellQ.UseAbility(v[z]);
                                    Utils.Sleep(350, Neutral[i].Handle.ToString() + "high_priest");
                                }
                            }
                        }
                    }
                }

                if (target == null)
                    return;

                if (target.IsAlive && !target.IsInvul() && (Game.MousePosition.Distance2D(target) <= 1000 || target.Distance2D(me) <= 600))
                {
                    var CheckStun = target.HasModifier("modifier_centaur_hoof_stomp");
                    var CheckSetka = target.HasModifier("modifier_dark_troll_warlord_ensnare");

                    for (int i = 0; i < Neutral.Count(); i++)
                    {
                        if (Neutral[i].Name == "npc_dota_neutral_dark_troll_warlord")
                        {
                            if (target.Position.Distance2D(Neutral[i].Position) < 550 &&
                                (!CheckSetka || !CheckStun || !target.IsHexed() || !target.IsStunned()) &&
                                Neutral[i].Spellbook.SpellQ.CanBeCasted() &&
                                Utils.SleepCheck(Neutral[i].Handle.ToString() + "warlord"))
                            {
                                Neutral[i].Spellbook.SpellQ.UseAbility(target);
                                Utils.Sleep(450, Neutral[i].Handle.ToString() + "warlord");
                            }
                        }
                        else if (Neutral[i].Name == "npc_dota_neutral_big_thunder_lizard")
                        {
                            if (target.Position.Distance2D(Neutral[i].Position) < 250 &&
                                Neutral[i].Spellbook.SpellQ.CanBeCasted() &&
                                Utils.SleepCheck(Neutral[i].Handle.ToString() + "lizard"))
                            {
                                Neutral[i].Spellbook.SpellQ.UseAbility();
                                Utils.Sleep(450, Neutral[i].Handle.ToString() + "lizard");
                            }
                            if (target.Position.Distance2D(Neutral[i].Position) < 550 &&
                                Neutral[i].Spellbook.SpellW.CanBeCasted() &&
                                Utils.SleepCheck(Neutral[i].Handle.ToString() + "lizard"))
                            {
                                Neutral[i].Spellbook.SpellW.UseAbility();
                                Utils.Sleep(450, Neutral[i].Handle.ToString() + "lizard");
                            }
                        }
                        else if (Neutral[i].Name == "npc_dota_neutral_centaur_khan")
                        {
                            if (target.Position.Distance2D(Neutral[i].Position) < 200 &&
                                (!CheckSetka || !CheckStun || !target.IsHexed() || !target.IsStunned()) &&
                                Neutral[i].Spellbook.SpellQ.CanBeCasted() &&
                                Utils.SleepCheck(Neutral[i].Handle.ToString() + "centaur"))
                            {
                                Neutral[i].Spellbook.SpellQ.UseAbility();
                                Utils.Sleep(450, Neutral[i].Handle.ToString() + "centaur");
                            }
                        }
                        else if (Neutral[i].Name == "npc_dota_neutral_satyr_hellcaller")
                        {
                            if (target.Position.Distance2D(Neutral[i].Position) < 850 &&
                                Neutral[i].Spellbook.SpellQ.CanBeCasted() &&
                                Utils.SleepCheck(Neutral[i].Handle.ToString() + "satyr"))
                            {
                                Neutral[i].Spellbook.SpellQ.UseAbility(target);
                                Utils.Sleep(350, Neutral[i].Handle.ToString() + "satyr");
                            }
                        }
                        else if (Neutral[i].Name == "npc_dota_neutral_satyr_trickster")
                        {
                            if (target.Position.Distance2D(Neutral[i].Position) < 850 &&
                                Neutral[i].Spellbook.SpellQ.CanBeCasted() &&
                                Utils.SleepCheck(Neutral[i].Handle.ToString() + "satyr_trickster"))
                            {
                                Neutral[i].Spellbook.SpellQ.UseAbility(target);
                                Utils.Sleep(350, Neutral[i].Handle.ToString() + "satyr_trickster");
                            }
                        }
                        else if (Neutral[i].Name == "npc_dota_neutral_satyr_soulstealer")
                        {
                            if (target.Position.Distance2D(Neutral[i].Position) < 850 &&
                                Neutral[i].Spellbook.SpellQ.CanBeCasted() &&
                                Utils.SleepCheck(Neutral[i].Handle.ToString() + "satyrsoulstealer"))
                            {
                                Neutral[i].Spellbook.SpellQ.UseAbility(target);
                                Utils.Sleep(350, Neutral[i].Handle.ToString() + "satyrsoulstealer");
                            }
                        }
                        else if (Neutral[i].Name == "npc_dota_neutral_black_dragon")
                        {
                            if (target.Position.Distance2D(Neutral[i].Position) < 700 &&
                                Neutral[i].Spellbook.SpellQ.CanBeCasted() &&
                                Utils.SleepCheck(Neutral[i].Handle.ToString() + "dragonspawn"))
                            {
                                Neutral[i].Spellbook.SpellQ.UseAbility(target.Predict(600));
                                Utils.Sleep(350, Neutral[i].Handle.ToString() + "dragonspawn");
                            }
                        }
                        else if (Neutral[i].Name == "npc_dota_neutral_big_thunder_lizard")
                        {
                            if (target.Position.Distance2D(Neutral[i].Position) < 200 &&
                                Neutral[i].Spellbook.SpellQ.CanBeCasted() &&
                                Utils.SleepCheck(Neutral[i].Handle.ToString() + "lizard"))
                            {
                                Neutral[i].Spellbook.SpellQ.UseAbility();
                                Utils.Sleep(350, Neutral[i].Handle.ToString() + "lizard");
                            }
                            var v =
                                ObjectManager.GetEntities<Hero>()
                                    .Where(x => x.Team == me.Team && x.IsAlive && x.IsVisible && !x.IsIllusion)
                                    .ToList();
                            for (int z = 0; z < v.Count(); z++)
                            {
                                if (Neutral[i].Spellbook.SpellW.CanBeCasted() &&
                                    Neutral[i].Position.Distance2D(v[z]) <= 900)
                                {
                                    if (target.Position.Distance2D(v[z]) < v[z].AttackRange + 150 &&
                                        Utils.SleepCheck(Neutral[i].Handle.ToString() + "lizard"))
                                    {
                                        Neutral[i].Spellbook.SpellW.UseAbility(v[z]);
                                        Utils.Sleep(350, Neutral[i].Handle.ToString() + "lizard");
                                    }
                                }
                            }
                        }
                        else if (Neutral[i].Name == "npc_dota_neutral_mud_golem")
                        {
                            if (target.Position.Distance2D(Neutral[i].Position) < 850 &&
                                (!CheckSetka || !CheckStun || !target.IsHexed() || !target.IsStunned())
                                && Neutral[i].Spellbook.SpellQ.CanBeCasted() &&
                                Utils.SleepCheck(Neutral[i].Handle.ToString() + "golem"))
                            {
                                Neutral[i].Spellbook.SpellQ.UseAbility(target);
                                Utils.Sleep(350, Neutral[i].Handle.ToString() + "golem");
                            }
                        }
                        else if (Neutral[i].Name == "npc_dota_neutral_polar_furbolg_ursa_warrior")
                        {
                            if (target.Position.Distance2D(Neutral[i].Position) < 240 &&
                                Neutral[i].Spellbook.SpellQ.CanBeCasted() &&
                                Utils.SleepCheck(Neutral[i].Handle.ToString() + "ursa"))
                            {
                                Neutral[i].Spellbook.SpellQ.UseAbility();
                                Utils.Sleep(350, Neutral[i].Handle.ToString() + "ursa");
                            }
                        }
                        else if (Neutral[i].Name == "npc_dota_neutral_harpy_storm")
                        {
                            if (target.Position.Distance2D(Neutral[i].Position) < 900 &&
                                Neutral[i].Spellbook.SpellQ.CanBeCasted() &&
                                Utils.SleepCheck(Neutral[i].Handle.ToString() + "harpy"))
                            {
                                Neutral[i].Spellbook.SpellQ.UseAbility(target);
                                Utils.Sleep(350, Neutral[i].Handle.ToString() + "harpy");
                            }
                        }

                        if (Neutral[i].Distance2D(target) <= Neutral[i].AttackRange + 100 &&
                            (!Neutral[i].IsAttackImmune() || !target.IsAttackImmune())
                            && Neutral[i].NetworkActivity != NetworkActivity.Attack && Neutral[i].CanAttack() &&
                            Utils.SleepCheck(Neutral[i].Handle.ToString() + "Attack")
                            )
                        {
                            Neutral[i].Attack(target);
                            Utils.Sleep(150, Neutral[i].Handle.ToString() + "Attack");
                        }
                        else if ((!Neutral[i].CanAttack() || Neutral[i].Distance2D(target) >= 0) &&
                                 Neutral[i].NetworkActivity != NetworkActivity.Attack &&
                                 Neutral[i].Distance2D(target) <= 600 &&
                                 Utils.SleepCheck(Neutral[i].Handle.ToString() + "Move")
                            )
                        {
                            Neutral[i].Move(target.Predict(300));
                            Utils.Sleep(250, Neutral[i].Handle.ToString() + "Move");
                        }

                    }
                }
                Utils.Sleep(200, "creepactivated");
            }
        }

        private static void Game_OnWndProc(WndEventArgs args)
        {
            if (!Game.IsChatOpen)
            {
                if (Menu.Item("comboKey").GetValue<KeyBind>().Active)
                {
                    Active = true;
                }
                else
                {
                    Active = false;
                }
            }

            if (!Game.IsChatOpen)
            {
                if (Menu.Item("ccontrol").GetValue<KeyBind>().Active)
                {
                    CreepActive = true;
                }
                else
                {
                    CreepActive = false;
                }
            }
        }

        public static void Drawing_OnDraw(EventArgs args)
        {
            if (!Game.IsInGame || Game.IsPaused || Game.IsWatchingGame)
                return;

            me = ObjectManager.LocalHero;

            target = me.ClosestToMouseTarget(3500);

            if (me == null || me.ClassID != ClassID.CDOTA_Unit_Hero_Enchantress)
                return;

            if (attackdragon != attackdragondraw)
            {
                attackdragondraw = attackdragon;
                if (DrawDragon.TryGetValue(3, out drawDragon))
                {
                    drawDragon.Dispose();
                    DrawDragon.Remove(3);
                }
                if (!DrawDragon.TryGetValue(3, out drawDragon))
                {
                    drawDragon = me.AddParticleEffect(@"particles\ui_mouseactions\drag_selected_ring.vpcf");
                    drawDragon.SetControlPoint(1, new Vector3(255, 150, 0));
                    drawDragon.SetControlPoint(2, new Vector3(800, 255, 0));
                    DrawDragon.Add(3, drawDragon);
                }
            }

            if (attackaghs != attackaghsdraw)
            {
                attackaghsdraw = attackaghs;
                if (DrawAghs.TryGetValue(3, out drawAghs))
                {
                    drawAghs.Dispose();
                    DrawAghs.Remove(3);
                }
                if (!DrawAghs.TryGetValue(3, out drawAghs))
                {
                    drawAghs = me.AddParticleEffect(@"particles\ui_mouseactions\drag_selected_ring.vpcf");
                    drawAghs.SetControlPoint(1, new Vector3(0, 255, 0));
                    drawAghs.SetControlPoint(2, new Vector3(850, 255, 0));
                    DrawAghs.Add(3, drawAghs);
                }
            }

            if (attackdraghs != attackdraghsdraw)
            {
                attackdraghsdraw = attackdraghs;
                if (DrawDraghs.TryGetValue(3, out drawDraghs))
                {
                    drawDraghs.Dispose();
                    DrawDraghs.Remove(3);
                }
                if (!DrawDraghs.TryGetValue(3, out drawDraghs))
                {
                    drawDraghs = me.AddParticleEffect(@"particles\ui_mouseactions\drag_selected_ring.vpcf");
                    drawDraghs.SetControlPoint(1, new Vector3(0, 150, 255));
                    drawDraghs.SetControlPoint(2, new Vector3(1005, 255, 0));
                    DrawDraghs.Add(3, drawDraghs);
                }
            }

            if (Menu.Item("drawattackdragon").GetValue<bool>())
            {
                if (!DrawDragon.TryGetValue(3, out drawDragon))
                {
                    drawDragon = me.AddParticleEffect(@"particles\ui_mouseactions\drag_selected_ring.vpcf");
                    drawDragon.SetControlPoint(1, new Vector3(255, 150, 0));
                    drawDragon.SetControlPoint(2, new Vector3(800, 255, 0));
                    DrawDragon.Add(3, drawDragon);
                }
            }
            else
            {
                if (DrawDragon.TryGetValue(3, out drawDragon))
                {
                    drawDragon.Dispose();
                    DrawDragon.Remove(3);
                }
            }

            if (Menu.Item("drawattackaghs").GetValue<bool>())
            {
                if (!DrawAghs.TryGetValue(3, out drawAghs))
                {
                    drawAghs = me.AddParticleEffect(@"particles\ui_mouseactions\drag_selected_ring.vpcf");
                    drawAghs.SetControlPoint(1, new Vector3(0, 255, 0));
                    drawAghs.SetControlPoint(2, new Vector3(850, 255, 0));
                    DrawAghs.Add(3, drawAghs);
                }
            }
            else
            {
                if (DrawAghs.TryGetValue(3, out drawAghs))
                {
                    drawAghs.Dispose();
                    DrawAghs.Remove(3);
                }
            }

            if (Menu.Item("drawattackdraghs").GetValue<bool>())
            {
                if (!DrawDraghs.TryGetValue(3, out drawDraghs))
                {
                    drawDraghs = me.AddParticleEffect(@"particles\ui_mouseactions\drag_selected_ring.vpcf");
                    drawDraghs.SetControlPoint(1, new Vector3(0, 150, 255));
                    drawDraghs.SetControlPoint(2, new Vector3(1005, 255, 0));
                    DrawDraghs.Add(3, drawDraghs);
                }
            }
            else
            {
                if (DrawDraghs.TryGetValue(3, out drawDraghs))
                {
                    drawDraghs.Dispose();
                    DrawDraghs.Remove(3);
                }
            }
        }

        private static void OnLoadMessage()
        {
            Game.PrintMessage("<font face='helvetica' color='#00FF00'>EnchantressSharp by Dash Loaded! </font>",
                MessageType.LogMessage);
        }
    }
}