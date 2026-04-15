using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using System.Collections.Generic;
using System;
using UI.Utility;

namespace BetterTeamKill;


[BepInPlugin(GUID, PLUGIN_NAME, VERSION)]
[BepInDependency("Lethe", BepInDependency.DependencyFlags.HardDependency)]
public class Main : BasePlugin
{
    public const string GUID = $"{AUTHOR}.{PLUGIN_NAME}";
    public const string PLUGIN_NAME = "BetterTeamKill";
    public const string VERSION = "1.0.0";
    public const string AUTHOR = "Doppelglower";
    public static Harmony harmony = new(GUID);
    public static ManualLogSource sharedLog;
    public static List<int> slotIDs = [];

    public override void Load()
    {
        sharedLog = Log;
        sharedLog.LogInfo($"{PLUGIN_NAME} loaded.");
        harmony.PatchAll(typeof(Main));
    }

    [HarmonyPatch(typeof(BattleUnitModel), nameof(BattleUnitModel.OnRoundStart_After_Event))]
    [HarmonyPostfix]
    public static void Postfix_BattleUnitModel_OnRoundStart_After_Event(BattleUnitModel __instance)
    {
        if (!__instance.IsFaction(UNIT_FACTION.PLAYER))
        {
            return;
        }
        var battleObjectManager = BattleObjectManager.Instance;
        if (battleObjectManager == null)
        {
            return;
        }
        var battleUIRoot = BattleUI.BattleUIRoot.Instance;
        if (battleUIRoot == null)
        {
            return;
        }
        var unitUIManager = battleObjectManager.GetView(__instance).UIManager;
        if (unitUIManager == null)
        {
            return;
        }
        var battleActionModelManager = BattleActionModelManager.Instance;
        if (battleActionModelManager == null)
        {
            return;
        }
        foreach (var unitActionSlotUI in unitUIManager.UnitActionUI._actionSlotUIList)
        {
            var instanceID = unitActionSlotUI.GetInstanceID();
            if (!slotIDs.Contains(instanceID))
            {
                slotIDs.Add(instanceID);
                unitActionSlotUI._trigger.AttachEntry(UnityEngine.EventSystems.EventTriggerType.PointerEnter, new Action(() =>
                {
                    battleUIRoot.ClearAllArrows();
                    SoundGenerator.PlayUISound(UI_SOUNDS_TYPE.BattleUI_CharacterSlot_Click);

                    if (battleUIRoot.AbUIController.IsDragginSin())
                    {
                        battleUIRoot.AbUIController._abOperationTracker._opDragginData._targetSinActionModel = unitActionSlotUI._sinAction;
                        battleUIRoot.NewOperationController.UpdateDraggingSlotFromOpSlotToSinAction();
                        UnitSinModel dragginSin = battleUIRoot.AbUIController.GetDragginSin();
                        battleUIRoot.CreateExpectedArrowByActionOver(dragginSin.GetBattleActionModel().SinAction, unitActionSlotUI._sinAction);
                        battleUIRoot.ShowExpectedSkillInfoByOverAction(dragginSin, unitActionSlotUI._sinAction);
                        if (unitActionSlotUI._sinAction.currentSelectSin != null)
                        {
                            battleUIRoot.ShowSkillInfoByOperation(unitActionSlotUI._sinAction.currentSelectSin, dragginSin.GetBattleActionModel().SinAction);
                        }
                        if (unitActionSlotUI._sinAction.CurrentBattleAction != null)
                        {
                            var currentBattleAction = unitActionSlotUI._sinAction.CurrentBattleAction;
                            var enemyBattleAction = dragginSin.GetBattleActionModel();

                            if (currentBattleAction.GetMainTargetSinAction() == dragginSin.GetBattleActionModel().SinAction && BattleActionModel.CanDuelBoth(dragginSin.GetBattleActionModel(), currentBattleAction))
                            {
                                battleActionModelManager.RemoveDuel(currentBattleAction);
                                battleActionModelManager.AddDuel(currentBattleAction, enemyBattleAction);
                            }
                            else if (BattleActionModel.CanDuelBoth(dragginSin.GetBattleActionModel(), currentBattleAction))
                            {
                                battleActionModelManager.RemoveDuel(currentBattleAction);
                                battleActionModelManager.AddDuel(currentBattleAction, enemyBattleAction);
                                //if (currentBattleAction.IsDone && enemyBattleAction.IsDone)
                                //{
                                //    currentBattleAction._targetDataDetail.GetCurrentTargetSet()._mainTarget = new TargetSinActionData(dragginSin._currentAction._sinAction);
                                //    dragginSin.GetBattleActionModel()._targetDataDetail.GetCurrentTargetSet()._mainTarget = new TargetSinActionData(unitActionSlotUI._sinAction); }
                                //}
                            }
                        }
                        else if (battleUIRoot.AbUIController.IsClickedOpSlot())
                        {
                            battleUIRoot.AbUIController._abOperationTracker._opDragginData._targetSinActionModel = unitActionSlotUI._sinAction;
                            UnitSinModel clickedSin = battleUIRoot.AbUIController.GetClickedSin();
                            if (unitActionSlotUI._sinAction != null)
                            {
                                battleUIRoot.CreateExpectedArrowByActionOver(clickedSin.GetBattleActionModel().SinAction, unitActionSlotUI._sinAction);
                                battleUIRoot.ShowExpectedSkillInfoByOverAction(clickedSin, unitActionSlotUI._sinAction);
                            }
                            else
                            {
                                battleUIRoot.ShowExpectedSkillInfoByOverAction(clickedSin);
                            }
                            if (unitActionSlotUI._sinAction.currentSelectSin != null)
                            {
                                battleUIRoot.ShowSkillInfoByOperation(unitActionSlotUI._sinAction.currentSelectSin, clickedSin.GetBattleActionModel().SinAction);
                            }
                            if (unitActionSlotUI._sinAction.CurrentBattleAction != null)
                            {
                                var currentBattleAction = unitActionSlotUI._sinAction.CurrentBattleAction;
                                var enemyBattleAction = clickedSin.GetBattleActionModel();
                                if (currentBattleAction.GetMainTargetSinAction() == clickedSin.GetBattleActionModel().SinAction && BattleActionModel.CanDuelBoth(clickedSin.GetBattleActionModel(), currentBattleAction))
                                {
                                    battleActionModelManager.RemoveDuel(currentBattleAction);
                                    battleActionModelManager.AddDuel(currentBattleAction, enemyBattleAction);
                                }

                                else if (BattleActionModel.CanDuelBoth(clickedSin.GetBattleActionModel(), currentBattleAction))
                                {
                                    battleActionModelManager.RemoveDuel(currentBattleAction);
                                    //battleActionModelManager.RemoveDuel(clickedSin.GetBattleActionModel());
                                    //currentBattleAction._targetDataDetail.GetCurrentTargetSet()._mainTarget = new TargetSinActionData(clickedSin._currentAction._sinAction);
                                    //clickedSin.GetBattleActionModel()._targetDataDetail.GetCurrentTargetSet()._mainTarget = new TargetSinActionData(unitActionSlotUI._sinAction);
                                    battleActionModelManager.AddDuel(currentBattleAction, enemyBattleAction);
                          
                                }

                            }
                            }
                        }
                        else
                        {
                            battleUIRoot.ClearHighlightAll();
                            BattleObjectManager.Instance.GetView(unitActionSlotUI._sinAction.UnitModel.InstanceID).ShowSkillInfoBySdOver();
                            battleUIRoot.ShowSkillInfoByUpperSlotOver(unitActionSlotUI._sinAction);
                            if (unitActionSlotUI._sinAction.currentSelectSin != null)
                            {
                                battleUIRoot.ShowSkillInfoByOperation(unitActionSlotUI._sinAction.currentSelectSin, (unitActionSlotUI._sinAction.currentSelectSin.GetBattleActionModel().GetTargetSinActionList().Count <= 0) ? null : unitActionSlotUI._sinAction.currentSelectSin.GetBattleActionModel().GetTargetSinActionList()[0]);
                            }
                        }
                    }
                ));
                unitActionSlotUI._trigger.AttachEntry(UnityEngine.EventSystems.EventTriggerType.PointerExit, new Action(() =>
                {
                    battleUIRoot.ClearAllArrows();
                    battleUIRoot.AbUIController.ClearDragginTargetData(updateOperation: true);

                    if (battleUIRoot.AbUIController.IsDragginSin() || battleUIRoot.AbUIController.IsClickedOpSlot())
                    {
                        battleUIRoot.ShowAbnormalityBattleDragSin();
                        if (battleUIRoot.AbUIController.IsDragginSin())
                        {
                            battleUIRoot.OffSkillInfo();
                            UnitSinModel dragginSin = battleUIRoot.AbUIController.GetDragginSin();
                            battleUIRoot.ShowSkillInfoByOperation(dragginSin, unitActionSlotUI._sinAction);
                            battleUIRoot.AbUIController._abOperationTracker._opDragginData._targetSinActionModel = null;
                        }
                        else if (battleUIRoot.AbUIController.IsClickedOpSlot())
                        {
                            battleUIRoot.OffSkillInfo();
                            UnitSinModel clickedSin = battleUIRoot.AbUIController.GetClickedSin();
                            battleUIRoot.ShowSkillInfoByOperation(clickedSin, unitActionSlotUI._sinAction);
                            battleUIRoot.AbUIController._abOperationTracker._opDragginData._targetSinActionModel = null;
                        }
                    }
                    else
                    {
                        BattleEffectManager.Instance.SetFadeBlackBackground_BattleView(value: false, commandState: true);
                        battleUIRoot.OffSkillInfo();
                        battleUIRoot.ShowAllCharacterTargetArrows();
                    }
                }));
                unitActionSlotUI._trigger.AttachEntry(UnityEngine.EventSystems.EventTriggerType.PointerClick, new Action(() =>
                {
                    if (battleUIRoot.AbUIController.IsDragginSin())
                    {
                        UnitSinModel dragginSin = battleUIRoot.AbUIController.GetDragginSin();
                        battleUIRoot.OffSkillInfo();
                        battleUIRoot.NewOperationController.EndDrag(dragginSin);
                        battleUIRoot.AbUIController._abOperationTracker.EndActionClicked();
                    }
                    else if (battleUIRoot.AbUIController.IsClickedOpSlot())
                    {
                        UnitSinModel clickedSin = battleUIRoot.AbUIController.GetClickedSin();
                        battleUIRoot.OffSkillInfo();
                        SinActionModel sinAction = clickedSin.GetBattleActionModel().SinAction;
                        sinAction.SelectSin(clickedSin, unitActionSlotUI._sinAction);
                        battleUIRoot.NewOperationController.EndDrag(clickedSin);
                        battleUIRoot.AbUIController._abOperationTracker.EndActionClicked();
                    }
                }));
            }
        }
    }

    [HarmonyPatch(typeof(SinActionModel), nameof(SinActionModel.IsTargetable))]
    [HarmonyPostfix]
    public static void Postfix_SinActionModel_IsTargetable(SinActionModel __instance, ref bool __result)
    {
        if (__instance.GetFaction() == UNIT_FACTION.PLAYER)
        {
            __result = true;
        }
    }

    [HarmonyPatch(typeof(BattleUnitModel), nameof(BattleUnitModel.IsTargetable))]
    [HarmonyPostfix]
    public static void Postfix_BattleUnitModel_IsTargetable(BattleUnitModel __instance, ref bool __result)
    {
        if (__instance.IsFaction(UNIT_FACTION.PLAYER))
        {
            __result = true;
        }
    }

    [HarmonyPatch(typeof(StageModel), nameof(StageModel.Init))]
    [HarmonyPrefix]
    public static void Prefix_StageModel_Init()
    {
        slotIDs.Clear();
    }
}