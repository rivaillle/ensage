﻿using System;
using System.Linq;
using System.Windows.Input;
using Ensage;
using Ensage.Common;
using Ensage.Common.Extensions;
using SharpDX;
using System.Collections.Generic;

namespace SupportSharp
{
    internal class Program
    {
        private const Key toggleKey = Key.T;
        private const Key orbwalkKey = Key.Space;
        private const Key saveSelfKey = Key.Y;
        private static Hero me;
        private static Ability misticAbility;
        private static double[] misticDamagePerLevel = new double[] { 90, 180, 240, 360 };
        private static double healThreshold = 0.5;
        private static Entity fountain;
        private static bool loaded;
        private static ParticleEffect rangeDisplay;
        private static double soulRingThreshold = 0.6;
        private static Item Urn,
            Meka,
            Guardian,
            Arcane,
            LotusOrb,
            Medallion,
            SolarCrest,
            GlimmerCape,
            Pipe,
            soulRing,
            Quelling,
            Stick,
            Wand,
            CrimsonGuard;

        private static Hero needMana;
        private static Hero needMeka;
        private static Hero target;
        private static bool supportActive;
        private static bool includeSaveSelf;
        private static bool shouldCastLotusOrb;
        private static bool shouldCastGlimmerCape;
        private static IEnumerable<Hero> enemies;
        private static void Main(string[] args)
        {
            Game.OnUpdate += Game_OnUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            /*Items*/
            Urn = null;
            Meka = null;
            Guardian = null;
            Arcane = null;
            LotusOrb = null;
            Medallion = null;
            SolarCrest = null;
            GlimmerCape = null;
            Pipe = null;
            CrimsonGuard = null;
            soulRing = null;
            loaded = false;
            supportActive = true;
            includeSaveSelf = false;
            shouldCastLotusOrb = false;
            shouldCastGlimmerCape = false;
            enemies = null;
            misticAbility = null;
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (loaded)
            {
                var mode = supportActive ? "ON" : "OFF";
                var orbwalkMode = Game.IsKeyDown(orbwalkKey) ? "ON" : "OFF";
                var includeSelfMode = includeSaveSelf ? "ON" : "OFF";
                Drawing.DrawText("Auto Support is: " + mode + ". Hotkey (Toggle): " + toggleKey + "",
                    new Vector2(Drawing.Width * 5 / 100, Drawing.Height * 4 / 100), Color.LightGreen, FontFlags.DropShadow);
                Drawing.DrawText("Orbwalk is: " + orbwalkMode + ". Hotkey (HOLD): " + orbwalkKey + "",
                    new Vector2(Drawing.Width * 5 / 100, Drawing.Height * 6 / 100), Color.LightGreen, FontFlags.DropShadow);
                Drawing.DrawText(
                    "Include Saving yourself?: " + includeSelfMode + ". Hotkey (TOGGLE): " + saveSelfKey + "",
                    new Vector2(Drawing.Width * 5 / 100, Drawing.Height * 8 / 100), Color.LightGreen, FontFlags.DropShadow);
                if(rangeDisplay == null)
                {
                    rangeDisplay = me.AddParticleEffect(@"particles\ui_mouseactions\drag_selected_ring.vpcf");
                    rangeDisplay.SetControlPoint(1, new Vector3(255, 255, 255));
                    rangeDisplay.SetControlPoint(2, new Vector3(600, 255, 0));
                }
               
            }
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            if (!loaded)
            {
                me = ObjectMgr.LocalHero;


                if (!Game.IsInGame || Game.IsWatchingGame || me == null || Game.IsChatOpen)
                {
                    return;
                }
                loaded = true;
            }

            if (me == null || !me.IsValid)
            {
                loaded = false;
                me = ObjectMgr.LocalHero;
                supportActive = false;
                includeSaveSelf = false;
                shouldCastLotusOrb = false;
                shouldCastGlimmerCape = false;
                return;
            }

            if (Game.IsPaused)
            {
                return;
            }

            Meka = me.FindItem("item_mekansm");
            Guardian = me.FindItem("item_guardian_greaves");
            Arcane = me.FindItem("item_arcane_boots");
            Medallion = me.FindItem("item_medallion_of_courage");
            SolarCrest = me.FindItem("item_solar_crest");
            soulRing = me.FindItem("item_soul_ring");
            Quelling = me.FindItem("item_quelling_blade");
            Stick = me.FindItem("item_magic_stick");
            Wand = me.FindItem("item_magic_wand");
            if (Quelling == null)
            {
                Quelling = me.FindItem("item_iron_talon");
            }
            Pipe = null;//me.FindItem("item_pipe");
            misticAbility = me.Spellbook.SpellQ;
            needMana = null;
            needMeka = null;

            if((soulRing != null && Utils.SleepCheck("HealSpell") && soulRing.CanBeCasted()) || me.Mana > 600)
            {
                healThreshold = 0.75;
            }else
            {
                healThreshold = 0.35;
            }

            if (!Game.IsChatOpen)
            {
                if (Game.IsKeyDown(toggleKey) && Utils.SleepCheck("togglingoption"))
                {
                    if (!supportActive)
                    {
                        supportActive = true;
                    }
                    else
                    {
                        supportActive = false;
                    }
                    Utils.Sleep(100 + Game.Ping, "togglingoption");
                }

                if (Game.IsKeyDown(saveSelfKey) && Utils.SleepCheck("togglingoption"))
                {
                    if (!includeSaveSelf)
                    {
                        includeSaveSelf = true;
                    }
                    else
                    {
                        includeSaveSelf = false;
                    }
                    Utils.Sleep(100 + Game.Ping, "togglingoption");
                }
            }

            if (supportActive)
            {
                double misticDamage = 0;
                if (misticAbility.Level > 0)
                {
                    misticDamage = misticDamagePerLevel[misticAbility.Level - 1];
                }
                var extraRange = 0;
                if (me.HasModifier("modifier_item_aether_lens"))
                {
                    extraRange = 200;
                }               

                var myEnemyList =
                                    ObjectMgr.GetEntities<Hero>()
                                        .Where(
                                            x =>
                                                x.Team != me.Team && !x.IsIllusion && x.IsAlive &&
                                                me.Distance2D(x) <= misticAbility.CastRange + extraRange + 260)
                                        .ToList();

                if (myEnemyList.Any())
                {
                    foreach (var enemy in myEnemyList)
                    {
                        
                        
                            double totalDamage = misticDamage;
                            if (me.HasModifier("modifier_item_aether_lens"))
                            {
                                totalDamage += totalDamage * 0.05;
                            }
                            totalDamage += totalDamage * (me.Intelligence % 16) / 100;
                            if (totalDamage >= (enemy.Health + enemy.HealthRegeneration * 1))
                            {
                                var nearCreepList =
                                    ObjectManager.GetEntitiesFast<Unit>()
                                        .Where(
                                            x =>
                                                x.Team == me.Team && x.IsAlive &&
                                                me.Distance2D(x) <= misticAbility.CastRange + extraRange && enemy.Distance2D(x) <= 260)
                                        .ToList();
                                if (nearCreepList.Any())
                                {
                                    var nearCreep = nearCreepList.First();
                                    CastHeal(misticAbility, nearCreep, false);
                                }
                             }
                            //Console.WriteLine("total damage is:" + totalDamage);
                        
                    }
                }

                var allies =
                    ObjectManager.GetEntitiesFast<Hero>()
                        .Where(
                            ally =>
                                ally.Team == me.Team && me.ClassID != ally.ClassID && ally.IsAlive && !ally.IsIllusion && me.Distance2D(ally) <= 1500)
                        .ToList();

                enemies =
                    ObjectManager.GetEntitiesFast<Hero>()
                        .Where(
                            entity =>
                                entity.Team != me.Team && entity.IsAlive && entity.IsVisible && !entity.IsIllusion);

                fountain =
                    ObjectManager.GetEntitiesFast<Entity>()
                        .First(entity => entity.ClassID == ClassID.CDOTA_Unit_Fountain && entity.Team == me.Team);

                
                uint addedRange = 0;
                if (me.ClassID == ClassID.CDOTA_Unit_Hero_Omniknight)
                {
                    switch (me.ClassID)
                    {
                        case ClassID.CDOTA_Unit_Hero_Omniknight:
                            if (me.HasModifier("modifier_item_aether_lens"))
                            {
                                addedRange += 200;
                            }
                            if(enemies != null)
                            {
                                Save(me, me.Spellbook.SpellW, 350, me.Spellbook.SpellW.CastRange + addedRange);
                                Heal(me, me.Spellbook.SpellQ, new float[] { 100, 150, 200, 250 },
                                     me.Spellbook.SpellQ.CastRange + addedRange);
                                Miracle(me, me.Spellbook.SpellR);
                            }
                            break;                            
                    }
                }

                if (Quelling != null && Quelling.Cooldown == 0)
                {
                    var wards =
                                            ObjectManager.GetEntitiesFast<Unit>()
                                                .Where(
                                                    x =>
                                                        x.Team != me.Team && (x.ClassID == ClassID.CDOTA_NPC_Observer_Ward || x.ClassID == ClassID.CDOTA_NPC_Observer_Ward_TrueSight) &&
                                                        me.Distance2D(x) <= 475);
                    if (wards.Any() && Utils.SleepCheck("deward"))
                    {
                        Quelling.UseAbility(wards.First());
                        Utils.Sleep(1000, "deward");
                    }
                }
                if (IsInDangerHeal(me) && me.Health <= me.MaximumHealth * 0.4)
                {
                    if (Stick != null && Utils.SleepCheck("Stick") && Stick.CurrentCharges > 0 && Stick.Cooldown > 0)
                    {
                        Stick.UseAbility();
                        Utils.Sleep(1000, "Stick");
                    }
                    if (Wand != null && Utils.SleepCheck("Wand") && Wand.CurrentCharges > 0)
                    {
                        Wand.UseAbility();
                        Utils.Sleep(1000, "Wand");
                    }

                }
            }
        }

        private static void Save(Hero self, Ability saveSpell, float castTime = 800, uint castRange = 0)
        {
            if (saveSpell != null && saveSpell.CanBeCasted() && self.IsAlive && !self.IsChanneling() && !self.IsInvisible())
            {
              
                var allies =
                    ObjectManager.GetEntitiesFast<Hero>()
                        .Where(
                            x =>
                                x.Team == self.Team && self.Distance2D(x) <= castRange && IsInDangerRepel(x) && !x.IsIllusion && x.IsAlive);
                     
                if (allies.Any())
                {
                    foreach (var ally in allies)
                    {
                        
                        if (Utils.SleepCheck("saveduration") && saveSpell.CanBeCasted())
                        {
                            saveSpell.UseAbility(ally);
                            Utils.Sleep(1000, "saveduration");
                        }
                        
                    }
                }
                                    
            }
        }

        private static void Heal(Hero self, Ability healSpell, float[] amount, uint range)
        {
            if (healSpell != null && healSpell.CanBeCasted() && !self.IsChanneling())
            {
                if (self.IsAlive && !self.IsChanneling() &&
                    (!self.IsInvisible() || !me.Modifiers.Any(x => x.Name == "modifier_treant_natures_guise")) &&
                    self.Distance2D(fountain) > 1000)
                {
                    var allies = ObjectManager.GetEntitiesFast<Hero>().Where(hero => hero.IsAlive && !hero.IsIllusion && hero.ClassID != me.ClassID && hero.Team == me.Team);

                    if (allies.Any())
                    {
                        foreach (var ally in allies)
                        {
                            if (ally.Health <= (ally.MaximumHealth * healThreshold) && healSpell.CanBeCasted() &&
                                self.Distance2D(fountain) > 1000 && IsInDangerHeal(ally) &&
                                ally.Health + amount[healSpell.Level - 1] <= ally.MaximumHealth)
                            {
                                CastHeal(healSpell, ally);                               
                                Utils.Sleep(1000 + Game.Ping, "ToggleHeal");
                                
                            }
                           
                        }
                    }
                }
            }
        }

        private static void CastHeal(Ability healSpell, Unit destination = null, bool toggle = false)
        {
            if (healSpell != null && healSpell.CanBeCasted() && me.CanCast())
            {
                
                if (destination != null)
                {
               
                    if (Utils.SleepCheck("HealSpell"))
                    {
                        if (soulRing != null && soulRing.CanBeCasted() && me.Health >= me.MaximumHealth * soulRingThreshold)
                        {
                            if (me.Health >= me.MaximumHealth * soulRingThreshold)
                            {
                                soulRing.UseAbility();
                            }
                        }
                        healSpell.UseAbility(destination);
                        Utils.Sleep(1000 + Game.Ping, "HealSpell");
                    }
                }
                
            }
        }

        private static bool IsInDangerRepel(Hero ally)
        {
            if (ally != null && ally.IsAlive && ally.ClassID != me.ClassID)
            {               
                
                foreach(var enemy in enemies)
                {
                    if(enemy.Distance2D(ally) < 1000 && IsFacing(enemy, ally))
                    {
                        if (enemy.IsChanneling())
                        {
                            return true;
                        }
                        var spell = enemy.Spellbook.Spells.Any(x => (x.AbilityType == AbilityType.Ultimate || x.IsDisable()) && x.IsInAbilityPhase);
                        if (spell)
                        {
                            return true;
                        }
                    }
                }/*
                foreach (var modifier in ally.Modifiers.ToList())
                {
                    Console.WriteLine(modifier.Name);
                }*/
                var buffs = new[]
                {
                    "modifier_doom_bringer_doom", "modifier_venomancer_venomous_gale",
                    "modifier_silencer_last_word", "modifier_bane_fiends_grip",
                    "modifier_earth_spirit_magnetize",
                    "modifier_batrider_flaming_lasso", "modifier_sniper_assassinate", "modifier_pudge_dismember",
                    "modifier_enigma_black_hole_pull", "modifier_disruptor_static_storm", "modifier_sand_king_epicenter",
                    "modifier_bloodseeker_rupture", "modifier_slark_pounce_leash"                  
                };
                foreach (var buff in buffs)
                {
                    if (ally.HasModifier(buff))
                    {
                        //Console.WriteLine("has modifier returning true");
                        return true;
                    }

                }
                var buffs2 = ally.Modifiers.ToList();

                if (buffs2.Any())
                {

                    foreach (var buff in buffs2)
                    {
                        //Console.WriteLine(ally.Name + " has modifier: " + buff.Name);
                    }

                }
                else
                {
                    //Console.WriteLine(ally.Name + " does not have any buff");
                }

                if(ally.IsSilenced() || ally.IsHexed() || ally.IsRooted())
                {                    
                    return true;
                }

                return false;
            }
            return false;
        }

        private static bool IsInDangerHeal(Hero ally)
        {
            if (ally != null && ally.IsAlive)
            {    
                if (enemies.Any())
                {
                    foreach (var enemy in enemies)
                    {
                        if (ally.Distance2D(enemy) < enemy.AttackRange + 50)
                        {
                            return true;
                        }
                        if (enemy.Spellbook.Spells.Any(abilities => ally.Distance2D(enemy) < abilities.CastRange + 50))
                        {
                            return true;
                        }
                    }
                }

                var buffs = new[]
                {
                    "modifier_item_urn_damage", "modifier_doom_bringer_doom", "modifier_axe_battle_hunger",
                    "modifier_queenofpain_shadow_strike", "modifier_phoenix_fire_spirit_burn",
                    "modifier_venomancer_poison_nova", "modifier_venomancer_venomous_gale",
                    "modifier_silencer_curse_of_the_silent", "modifier_silencer_last_word", "modifier_bane_fiends_grip",
                    "modifier_earth_spirit_magnetize", "modifier_jakiro_macropyre", "modifier_nerolyte_reapers_scythe",
                    "modifier_batrider_flaming_lasso", "modifier_sniper_assassinate", "modifier_pudge_dismember",
                    "modifier_enigma_black_hole_pull", "modifier_disruptor_static_storm", "modifier_sand_king_epicenter",
                    "modifier_bloodseeker_rupture", "modifier_dual_breath_burn", "modifier_jakiro_liquid_fire_burn",
                    "modifier_axe_battle_hunger", "modifier_viper_corrosive_skin", "modifier_viper_poison_attack",
                    "modifier_viper_viper_strike", "modifier_bounty_hunter_track", "modifier_life_stealer_open_wounds",
                    "modifier_phantom_assassin_stiflingdagger", "modifier_phantom_lancer_spirit_lance"
                };
                foreach (var buff in buffs)
                {
                    if (ally.HasModifier(buff))
                    {
                        //Console.WriteLine("has modifier returning true");
                        return true;
                    }

                }

                var buffs2 = ally.Modifiers.ToList();

                if (false && buffs2.Any())
                {

                    foreach (var buff in buffs2)
                    {
                        //Console.WriteLine(ally.Name + " has modifier: " + buff.Name);
                    }

                }
                else
                {
                    //Console.WriteLine(ally.Name + " does not have any buff");
                }

                if (ally.IsStunned() || ally.IsSilenced())
                {
                    return true;
                }
                if ((ally.IsStunned() ||
                     (ally.IsSilenced() &&
                      ((ally.FindItem("item_manta_style") == null || ally.FindItem("item_manta_style").Cooldown > 0) ||
                       (ally.FindItem("item_black_king_bar") == null ||
                        ally.FindItem("item_black_king_bar").Cooldown > 0))) ||
                     ally.IsHexed() ||
                     ally.IsRooted()) && !ally.IsInvul()
                    )
                {
                    //Console.WriteLine("stun detected!");
                    return true;
                }

                return false;
            }
            return false;
        }

        private static void Miracle(Hero self, Ability miracleSpell, uint radius = 600)
        {
            if (miracleSpell != null && miracleSpell.CanBeCasted() && self.IsAlive && !self.IsChanneling() && !self.IsInvisible())
            {
                if (enemies.Any())
                {
                    foreach (var enemy in enemies)
                    {
                        if (miracleSpell.CanBeCasted() && Utils.SleepCheck("miracle"))
                        {
                            if (enemy.ClassID == ClassID.CDOTA_Unit_Hero_Juggernaut && enemy.Distance2D(me) <= 600 && (enemy.HasModifier("modifier_juggernaut_omnislash") || enemy.Spellbook.SpellR.IsInAbilityPhase) && Utils.SleepCheck("miracle"))
                            {
                                miracleSpell.UseAbility();
                                Utils.Sleep(4000 + Game.Ping, "miracle");

                            }
                            var blink = enemy.FindItem("item_blink");
                            if (blink != null && blink.Cooldown > 8)
                            {
                                if (!((enemy.ClassID == ClassID.CDOTA_Unit_Hero_Sven && !enemy.HasModifier("modifier_sven_gods_strength")) || (enemy.ClassID == ClassID.CDOTA_Unit_Hero_Legion_Commander && !(enemy.Spellbook.SpellR.CanBeCasted()))))
                                {
                                    miracleSpell.UseAbility();
                                    Utils.Sleep(4000 + Game.Ping, "crimson");
                                }

                            }
                        }

                    }
                }

            }
        }

        private static bool IsFacing(Hero hero, Hero enemy)
        {

            float deltaY = hero.Position.Y - enemy.Position.Y;
            float deltaX = hero.Position.X - enemy.Position.X;
            float angle = (float)(Math.Atan2(deltaY, deltaX));

            float n1 = (float)Math.Sin(hero.RotationRad - angle);
            float n2 = (float)Math.Cos(hero.RotationRad - angle);

            return (Math.PI - Math.Abs(Math.Atan2(n1, n2))) < 0.15;
        }
    }
}