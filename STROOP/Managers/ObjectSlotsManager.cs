﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using STROOP.Structs;
using STROOP.Utilities;
using System.Drawing;
using STROOP.Extensions;
using STROOP.Structs.Configurations;
using STROOP.Controls;
using STROOP.Models;
using System.Collections.ObjectModel;

namespace STROOP.Managers
{
    public class ObjectSlotsManager
    {
        /// <summary>
        /// The default size of the object slot UI element
        /// </summary>
        const int DefaultSlotSize = 36;

        public enum TabType { Object, Map, Model, Custom, CamHack, Other };
        public enum SortMethodType { ProcessingOrder, MemoryOrder, DistanceToMario };
        public enum SlotLabelType { Recommended, SlotPosVs, SlotPos, SlotIndex }
        public enum ClickType { ObjectClick, MapClick, ModelClick, CamHackClick, MarkClick };

        public ObjectSlot HoveredOverSlot { get; private set; }

        public List<ObjectSlot> ObjectSlots;

        ObjectSlotManagerGui _gui;

        Dictionary<uint, Tuple<int?, int?>> _lockedSlotIndices = new Dictionary<uint, Tuple<int?, int?>>();
        public bool LabelsLocked = false;

        public List<uint> SelectedSlotsAddresses = new List<uint>();
        public List<uint> SelectedOnMapSlotsAddresses = new List<uint>();
        public List<uint> MarkedSlotsAddresses = new List<uint>();

        private Dictionary<uint, string> _slotLabels = new Dictionary<uint, string>();
        public IReadOnlyDictionary<uint, string> SlotLabelsForObjects { get; private set; }

        public TabType ActiveTab;
        public SortMethodType SortMethod = SortMethodType.ProcessingOrder;
        public SlotLabelType LabelMethod = SlotLabelType.Recommended;

        public ObjectSlotsManager(ObjectSlotManagerGui gui, TabControl tabControlMain)
        {
            _gui = gui;

            // Add SortMethods adn LabelMethods
            _gui.SortMethodComboBox.DataSource = Enum.GetValues(typeof(SortMethodType));
            _gui.LabelMethodComboBox.DataSource = Enum.GetValues(typeof(SlotLabelType));

            _gui.TabControl.Selected += TabControl_Selected;
            TabControl_Selected(this, new TabControlEventArgs(_gui.TabControl.SelectedTab, -1, TabControlAction.Selected));

            // Create and setup object slots
            ObjectSlots = new List<ObjectSlot>();
            foreach (int i in Enumerable.Range(0, ObjectSlotsConfig.MaxSlots))
            {
                var objectSlot = new ObjectSlot(this, i, _gui, new Size(DefaultSlotSize, DefaultSlotSize));
                objectSlot.Click += (sender, e) => OnSlotClick(sender, e);
                ObjectSlots.Add(objectSlot);
                _gui.FlowLayoutContainer.Controls.Add(objectSlot);
            };

            SlotLabelsForObjects = new ReadOnlyDictionary<uint, string>(_slotLabels);
        }

        public void ChangeSlotSize(int newSize)
        {
            foreach (var objSlot in ObjectSlots)
                objSlot.Size = new Size(newSize, newSize);
        }

        private static readonly Dictionary<string, TabType> TabNameToTabType = new Dictionary<string, TabType>()
        {
            ["Object"] = TabType.Object,
            ["Map"] = TabType.Map,
            ["Model"] = TabType.Model,
            ["Custom"] = TabType.Custom,
            ["Cam Hack"] = TabType.CamHack,
        };
        private void TabControl_Selected(object sender, TabControlEventArgs e)
        {
            TabType tabType;
            if (!TabNameToTabType.TryGetValue(e.TabPage.Text, out tabType))
                tabType = TabType.Other;
            ActiveTab = tabType;
        }

        private void OnSlotClick(object sender, EventArgs e)
        {
            // Make sure the tab has loaded
            if (_gui.TabControl.SelectedTab == null)
                return;

            ObjectSlot selectedSlot = sender as ObjectSlot;
            selectedSlot.Focus();

            bool isCtrlKeyHeld = Control.ModifierKeys == Keys.Control; 
            bool isShiftKeyHeld = Control.ModifierKeys == Keys.Shift;
            bool isAltKeyHeld = Control.ModifierKeys == Keys.Alt;

            DoSlotClickUsingInput(selectedSlot, isCtrlKeyHeld, isShiftKeyHeld, isAltKeyHeld);
        }

        private void DoSlotClickUsingInput(
            ObjectSlot selectedSlot, bool isCtrlKeyHeld, bool isShiftKeyHeld, bool isAltKeyHeld)
        {
            ClickType click = GetClickType(isAltKeyHeld);
            bool shouldToggle = ShouldToggle(isCtrlKeyHeld, isAltKeyHeld);
            bool shouldExtendRange = isShiftKeyHeld;
            DoSlotClickUsingSpecifications(selectedSlot, click, shouldToggle, shouldExtendRange);
        }

        public void SelectSlotByAddress(uint address)
        {
            ObjectSlot slot = ObjectSlots.FirstOrDefault(s => s.CurrentObject.Address == address);
            if (slot != null) DoSlotClickUsingInput(slot, false, false, false);
        }

        private ClickType GetClickType(bool isAltKeyHeld)
        {
            ClickType click;
            if (isAltKeyHeld)
            {
                click = ClickType.MarkClick;
            }
            else
            {
                switch (ActiveTab)
                {
                    case TabType.CamHack:
                        click = ClickType.CamHackClick;
                        break;

                    case TabType.Map:
                        click = ClickType.MapClick;
                        break;

                    case TabType.Model:
                        click = ClickType.ModelClick;
                        break;

                    case TabType.Object:
                    case TabType.Custom:
                    case TabType.Other:
                        click = ClickType.ObjectClick;
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            return click;
        }

        private bool ShouldToggle(bool isCtrlKeyHeld, bool isAltKeyHeld)
        {
            bool isTogglingTab = ActiveTab == TabType.Map || ActiveTab == TabType.CamHack;
            bool isToggleState = isAltKeyHeld ? true : isTogglingTab;
            return isToggleState != isCtrlKeyHeld;
        }

        private bool ShouldSwitchToObjTabByDefault()
        {
            return ActiveTab == TabType.Object || ActiveTab == TabType.Other;
        }

        public void DoSlotClickUsingSpecifications(
            ObjectSlot selectedSlot, ClickType click, bool shouldToggle, bool shouldExtendRange, bool? switchToObjTabNullable = null)
        {
            if (selectedSlot.CurrentObject == null)
                return;

            if (click == ClickType.ModelClick)
            {
                uint currentModelObjectAddress = Config.ModelManager.ModelObjectAddress;
                uint newModelObjectAddress = currentModelObjectAddress == selectedSlot.CurrentObject.Address ? 0 
                    : selectedSlot.CurrentObject.Address;
                Config.ModelManager.ModelObjectAddress = newModelObjectAddress;
                Config.ModelManager.ManualMode = false;
            }
            else if (click == ClickType.CamHackClick)
            {
                uint currentCamHackSlot = Config.Stream.GetUInt32(CameraHackConfig.CameraHackStruct + CameraHackConfig.ObjectOffset);
                uint newCamHackSlot = currentCamHackSlot == selectedSlot.CurrentObject.Address ? 0 
                    : selectedSlot.CurrentObject.Address;
                Config.Stream.SetValue(newCamHackSlot, CameraHackConfig.CameraHackStruct + CameraHackConfig.ObjectOffset);
            }
            else
            {
                List<uint> selection;
                switch (click)
                {
                    case ClickType.ObjectClick:
                        selection = SelectedSlotsAddresses;
                        break;
                    case ClickType.MapClick:
                        selection = SelectedOnMapSlotsAddresses;
                        break;
                    case ClickType.MarkClick:
                        selection = MarkedSlotsAddresses;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                bool switchToObjTab = switchToObjTabNullable ?? ShouldSwitchToObjTabByDefault();
                if (switchToObjTab)
                    _gui.TabControl.SelectedTab = _gui.TabControl.TabPages["tabPageObjects"];

                if (shouldExtendRange && selection.Count > 0)
                {
                    uint startRangeAddress = selection[selection.Count - 1];
                    int startRange = ObjectSlots.First(o => o.CurrentObject.Address == startRangeAddress).Index;
                    int endRange = selectedSlot.Index;

                    int rangeSize = Math.Abs(endRange - startRange);
                    int iteratorDirection = endRange > startRange ? 1 : -1;

                    for (int i = 0; i <= rangeSize; i++)
                    {
                        int index = startRange + i * iteratorDirection;
                        uint address = ObjectSlots[index].CurrentObject.Address;
                        if (!selection.Contains(address))
                            selection.Add(address);
                    }
                }
                else
                {
                    if (!shouldToggle)
                        selection.Clear();

                        if (selection.Contains(selectedSlot.CurrentObject.Address))
                            selection.Remove(selectedSlot.CurrentObject.Address);
                        else
                            selection.Add(selectedSlot.CurrentObject.Address);
                }

                if (click == ClickType.ObjectClick)
                    Config.ObjectManager.DisplayedObjects = ObjectSlots
                        .Where(s => selection.Contains(s.CurrentObject.Address))
                        .Select(s => s.CurrentObject.Address);
            }
        }
 
        public string GetSlotNameFromAddress(uint address)
        {
            ObjectSlot slot = ObjectSlots.FirstOrDefault(s => s.CurrentObject.Address == address);
            return slot?.Text;
        }

        public ObjectDataModel GetObjectFromName(string name)
        {
            if (name == null) return null;
            name = name.ToLower().Trim();
            ObjectSlot slot = ObjectSlots.FirstOrDefault(s => s.Text.ToLower() == name);
            return slot?.CurrentObject;
        }

        public void Update()
        {
            LabelMethod = (SlotLabelType)_gui.LabelMethodComboBox.SelectedItem;
            SortMethod = (SortMethodType) _gui.SortMethodComboBox.SelectedItem;

            // Lock label update
            LabelsLocked = _gui.LockLabelsCheckbox.Checked;

            ObjectSlot hoverObjectSlot = ObjectSlots.FirstOrDefault(s => s.IsHovering);

            // Processing sort order
            IEnumerable<ObjectDataModel> sortedObjects;
            switch (SortMethod)
            {
                case SortMethodType.ProcessingOrder:
                    // Data is already sorted by processing order
                    sortedObjects = DataModels.Objects.OrderBy(o => o.ProcessIndex);
                    break;

                case SortMethodType.MemoryOrder:
                    // Order by address
                    sortedObjects = DataModels.Objects.OrderBy(o => o.Address);
                    break;

                case SortMethodType.DistanceToMario:

                    // Order by address
                    var activeObjects = DataModels.Objects.Where(s => s.IsActive).OrderBy(s => s.DistanceToMarioCalculated);
                    var inActiveObjects = DataModels.Objects.Where(s => !s.IsActive).OrderBy(s => s.DistanceToMarioCalculated);

                    sortedObjects = activeObjects.Concat(inActiveObjects);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("Uknown sort method type");
            }

            // Update slots
            UpdateSlots(sortedObjects);
        }

        private void UpdateSlots(IEnumerable<ObjectDataModel> sortedObjects)
        {
            // Update labels
            if (!LabelsLocked)
            {
                foreach(ObjectDataModel obj in DataModels.Objects)
                    _lockedSlotIndices[obj.Address] = new Tuple<int?, int?>(obj.ProcessIndex, obj.VacantSlotIndex);
            }
            foreach (uint address in sortedObjects.Select(o => o.Address))
                _slotLabels[address] = GetSlotLabelForAddress(address);

            // Update object slots
            foreach (var item in sortedObjects.Zip(ObjectSlots, (o, s) => new { Slot = s, Obj = o }))
                item.Slot.Update(item.Obj);
        }

        public int GetSlotIndexFromAddres(uint objAddress)
        {
            return ObjectSlots.FirstOrDefault(o => o.CurrentObject.Address == objAddress)?.Index ?? -1;
        }

        public string GetSlotLabelForAddress(uint objAddress)
        {
            switch (LabelMethod)
            {
                case SlotLabelType.Recommended:
                    if (SortMethod == SortMethodType.MemoryOrder)
                        goto case SlotLabelType.SlotIndex;
                    else
                        goto case SlotLabelType.SlotPosVs;

                case SlotLabelType.SlotIndex:
                    return String.Format("{0}", (objAddress - ObjectSlotsConfig.LinkStartAddress)
                        / ObjectConfig.StructSize + (OptionsConfig.SlotIndexsFromOne ? 1 : 0));

                case SlotLabelType.SlotPos:
                    return String.Format("{0}", _lockedSlotIndices[objAddress].Item1
                        + (OptionsConfig.SlotIndexsFromOne ? 1 : 0));

                case SlotLabelType.SlotPosVs:
                    var vacantSlotIndex = _lockedSlotIndices[objAddress].Item2;
                    if (!vacantSlotIndex.HasValue)
                        goto case SlotLabelType.SlotPos;

                    return String.Format("VS{0}", vacantSlotIndex.Value
                        + (OptionsConfig.SlotIndexsFromOne ? 1 : 0));

                default:
                    return "";
            }
        }
    }
}
 