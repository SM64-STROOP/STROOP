﻿using STROOP.Controls;
using STROOP.Managers;
using STROOP.Models;
using STROOP.Structs.Configurations;
using STROOP.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace STROOP.Structs
{
    public static class WatchVariableSelectionUtilities
    {

        public static ContextMenuStrip CreateSelectionContextMenuStrip(WatchVariableFlowLayoutPanel panel)
        {
            Func<List<WatchVariableControl>> getVars = () => panel.GetCurrentlySelectedVariableControls();
            Action<WatchVariableControlSettings> apply =
                (WatchVariableControlSettings settings) => getVars().ForEach(control => control.ApplySettings(settings));

            ContextMenuStrip contextMenuStrip = new ContextMenuStrip();

            ToolStripMenuItem itemHighlight = new ToolStripMenuItem("Highlight...");
            ControlUtilities.AddDropDownItems(
                itemHighlight,
                new List<string>() { "Highlight", "Don't Highlight" },
                new List<Action>()
                {
                    () => apply(new WatchVariableControlSettings(changeHighlighted: true, newHighlighted: true)),
                    () => apply(new WatchVariableControlSettings(changeHighlighted: true, newHighlighted: false)),
                });

            ToolStripMenuItem itemLock = new ToolStripMenuItem("Lock...");
            ControlUtilities.AddDropDownItems(
                itemLock,
                new List<string>() { "Lock", "Don't Lock" },
                new List<Action>()
                {
                    () => apply(new WatchVariableControlSettings(changeLocked: true, newLocked: true)),
                    () => apply(new WatchVariableControlSettings(changeLocked: true, newLocked: false)),
                });

            ToolStripMenuItem itemCopy = new ToolStripMenuItem("Copy...");
            Action<List<WatchVariableControl>, string> copyValues =
                (List<WatchVariableControl> controls, string separator) =>
            {
                Clipboard.SetText(
                    String.Join(separator, controls.ConvertAll(
                        control => control.GetValue(false))));
            };
            ControlUtilities.AddDropDownItems(
                itemCopy,
                new List<string>() { "Copy with Commas", "Copy with Tabs", "Copy with Line Breaks" },
                new List<Action>()
                {
                    () => copyValues(getVars(), ","),
                    () => copyValues(getVars(), "\t"),
                    () => copyValues(getVars(), "\r\n"),
                });

            ToolStripMenuItem itemPaste = new ToolStripMenuItem("Paste");
            itemPaste.Click += (sender, e) =>
            {
                List<string> stringList = ParsingUtilities.ParseStringList(Clipboard.GetText());
                List<WatchVariableControl> varList = getVars();
                if (stringList.Count != varList.Count) return;

                Config.Stream.Suspend();
                for (int i = 0; i < stringList.Count; i++)
                {
                    varList[i].SetValue(stringList[i]);
                }
                Config.Stream.Resume();
            };

            ToolStripMenuItem itemRoundTo = new ToolStripMenuItem("Round to...");
            ToolStripMenuItem itemRoundToDefault = new ToolStripMenuItem("Default");
            itemRoundToDefault.Click += (sender, e) =>
                apply(new WatchVariableControlSettings(
                    changeRoundingLimit: true, changeRoundingLimitToDefault: true));
            ToolStripMenuItem itemRoundToNoRounding = new ToolStripMenuItem("No Rounding");
            itemRoundToNoRounding.Click += (sender, e) =>
                apply(new WatchVariableControlSettings(
                    changeRoundingLimit: true, newRoundingLimit: -1));
            List<ToolStripMenuItem> itemsRoundToNumDecimalPlaces = new List<ToolStripMenuItem>();
            for (int i = 0; i <= 10; i++)
            {
                int index = i;
                itemsRoundToNumDecimalPlaces.Add(new ToolStripMenuItem(index + " decimal place(s)"));
                itemsRoundToNumDecimalPlaces[index].Click += (sender, e) =>
                    apply(new WatchVariableControlSettings(
                        changeRoundingLimit: true, newRoundingLimit: index));
            }
            itemRoundTo.DropDownItems.Add(itemRoundToDefault);
            itemRoundTo.DropDownItems.Add(itemRoundToNoRounding);
            itemsRoundToNumDecimalPlaces.ForEach(setAllRoundingLimitsNumberItem =>
            {
                itemRoundTo.DropDownItems.Add(setAllRoundingLimitsNumberItem);
            });

            ToolStripMenuItem itemDisplayAsHex = new ToolStripMenuItem("Display as Hex...");
            ControlUtilities.AddDropDownItems(
                itemDisplayAsHex,
                new List<string>() { "Default", "Hex", "Decimal" },
                new List<Action>()
                {
                    () => apply(new WatchVariableControlSettings(changeDisplayAsHex: true, changeDisplayAsHexToDefault: true)),
                    () => apply(new WatchVariableControlSettings(changeDisplayAsHex: true, newDisplayAsHex: true)),
                    () => apply(new WatchVariableControlSettings(changeDisplayAsHex: true, newDisplayAsHex: false)),
                });

            ToolStripMenuItem itemAngleSigned = new ToolStripMenuItem("Angle: Signed...");
            ControlUtilities.AddDropDownItems(
                itemAngleSigned,
                new List<string>() { "Default", "Unsigned", "Signed" },
                new List<Action>()
                {
                    () => apply(new WatchVariableControlSettings(changeAngleSigned: true, changeAngleSignedToDefault: true)),
                    () => apply(new WatchVariableControlSettings(changeAngleSigned: true, newAngleSigned: false)),
                    () => apply(new WatchVariableControlSettings(changeAngleSigned: true, newAngleSigned: true)),
                });

            ToolStripMenuItem itemAngleUnits = new ToolStripMenuItem("Angle: Units...");
            ToolStripMenuItem itemAngleUnitsDefault = new ToolStripMenuItem("Default");
            itemAngleUnitsDefault.Click += (sender, e) =>
                apply(new WatchVariableControlSettings(
                    changeAngleUnits: true, changeAngleUnitsToDefault: true));
            List<ToolStripMenuItem> itemsAngleUnitsValue = new List<ToolStripMenuItem>();
            foreach (AngleUnitType angleUnitType in Enum.GetValues(typeof(AngleUnitType)))
            {
                AngleUnitType angleUnitTypeFixed = angleUnitType;
                ToolStripMenuItem itemAngleUnitsValue = new ToolStripMenuItem(angleUnitTypeFixed.ToString());
                itemAngleUnitsValue.Click += (sender, e) =>
                    apply(new WatchVariableControlSettings(
                        changeAngleUnits: true, newAngleUnits: angleUnitTypeFixed));
                itemsAngleUnitsValue.Add(itemAngleUnitsValue);
            }
            itemAngleUnits.DropDownItems.Add(itemAngleUnitsDefault);
            itemsAngleUnitsValue.ForEach(setAllAngleUnitsValuesItem =>
            {
                itemAngleUnits.DropDownItems.Add(setAllAngleUnitsValuesItem);
            });

            ToolStripMenuItem itemAngleTruncateToMultipleOf16 = new ToolStripMenuItem("Angle: Truncate to Multiple of 16...");
            ControlUtilities.AddDropDownItems(
                itemAngleTruncateToMultipleOf16,
                new List<string>() { "Default", "Truncate to Multiple of 16", "Don't Truncate to Multiple of 16" },
                new List<Action>()
                {
                    () => apply(new WatchVariableControlSettings(changeAngleTruncateToMultipleOf16: true, changeAngleTruncateToMultipleOf16ToDefault: true)),
                    () => apply(new WatchVariableControlSettings(changeAngleTruncateToMultipleOf16: true, newAngleTruncateToMultipleOf16: true)),
                    () => apply(new WatchVariableControlSettings(changeAngleTruncateToMultipleOf16: true, newAngleTruncateToMultipleOf16: false)),
                });

            ToolStripMenuItem itemAngleConstrainToOneRevolution = new ToolStripMenuItem("Angle: Constrain to One Revolution...");
            ControlUtilities.AddDropDownItems(
                itemAngleConstrainToOneRevolution,
                new List<string>() { "Default", "Constrain to One Revolution", "Don't Constrain to One Revolution" },
                new List<Action>()
                {
                    () => apply(new WatchVariableControlSettings(changeAngleConstrainToOneRevolution: true, changeAngleConstrainToOneRevolutionToDefault: true)),
                    () => apply(new WatchVariableControlSettings(changeAngleConstrainToOneRevolution: true, newAngleConstrainToOneRevolution: true)),
                    () => apply(new WatchVariableControlSettings(changeAngleConstrainToOneRevolution: true, newAngleConstrainToOneRevolution: false)),
                });

            ToolStripMenuItem itemAngleDisplayAsHex = new ToolStripMenuItem("Angle: Display as Hex...");
            ControlUtilities.AddDropDownItems(
                itemAngleDisplayAsHex,
                new List<string>() { "Default", "Hex", "Decimal" },
                new List<Action>()
                {
                    () => apply(new WatchVariableControlSettings(changeAngleDisplayAsHex: true, changeAngleDisplayAsHexToDefault: true)),
                    () => apply(new WatchVariableControlSettings(changeAngleDisplayAsHex: true, newAngleDisplayAsHex: true)),
                    () => apply(new WatchVariableControlSettings(changeAngleDisplayAsHex: true, newAngleDisplayAsHex: false)),
                });

            ToolStripMenuItem itemMove = new ToolStripMenuItem("Move");
            ToolStripMenuItem itemDelete = new ToolStripMenuItem("Delete");
            ToolStripMenuItem itemOpenController = new ToolStripMenuItem("Open Controller");
            ToolStripMenuItem itemAddToCustomTab = new ToolStripMenuItem("Add to Custom Tab");

            contextMenuStrip.Items.Add(itemHighlight);
            contextMenuStrip.Items.Add(itemLock);
            contextMenuStrip.Items.Add(itemCopy);
            contextMenuStrip.Items.Add(itemPaste);
            contextMenuStrip.Items.Add(new ToolStripSeparator());
            contextMenuStrip.Items.Add(itemRoundTo);
            contextMenuStrip.Items.Add(itemDisplayAsHex);
            contextMenuStrip.Items.Add(new ToolStripSeparator());
            contextMenuStrip.Items.Add(itemAngleSigned);
            contextMenuStrip.Items.Add(itemAngleUnits);
            contextMenuStrip.Items.Add(itemAngleTruncateToMultipleOf16);
            contextMenuStrip.Items.Add(itemAngleConstrainToOneRevolution);
            contextMenuStrip.Items.Add(itemAngleDisplayAsHex);
            contextMenuStrip.Items.Add(new ToolStripSeparator());
            contextMenuStrip.Items.Add(itemMove);
            contextMenuStrip.Items.Add(itemDelete);
            contextMenuStrip.Items.Add(itemOpenController);
            contextMenuStrip.Items.Add(itemAddToCustomTab);

            return contextMenuStrip;
        }
        
    }
}