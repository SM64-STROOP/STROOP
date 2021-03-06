﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using OpenTK.Graphics.OpenGL;
using STROOP.Utilities;
using STROOP.Structs.Configurations;
using STROOP.Structs;
using OpenTK;
using System.Windows.Forms;

namespace STROOP.Map
{
    public abstract class MapArrowObject : MapLineObject
    {
        private bool _useRecommendedArrowLength;
        private float _arrowHeadSideLength;
        private float _angleOffset;

        private ToolStripMenuItem _itemUseSpeedForArrowLength;
        private ToolStripMenuItem _itemSetArrowHeadSideLength;
        private ToolStripMenuItem _itemSetAngleOffset;

        private static readonly string SET_ARROW_HEAD_SIDE_LENGTH_TEXT = "Set Arrow Head Side Length";
        private static readonly string SET_ANGLE_OFFSET_TEXT = "Set Angle Offset";

        public MapArrowObject()
            : base()
        {
            _useRecommendedArrowLength = false;
            _arrowHeadSideLength = 100;
            _angleOffset = 0;

            Size = 300;
            OutlineWidth = 3;
            OutlineColor = Color.Yellow;
        }

        protected override List<(float x, float y, float z)> GetVerticesTopDownView()
        {
            PositionAngle posAngle = GetPositionAngle();
            float x = (float)posAngle.X;
            float y = (float)posAngle.Y;
            float z = (float)posAngle.Z;
            float yaw = (float)GetYaw() + _angleOffset;
            float size = _useRecommendedArrowLength ? (float)GetRecommendedSize() : Size;
            (float arrowHeadX, float arrowHeadZ) =
                ((float, float))MoreMath.AddVectorToPoint(size, yaw, x, z);

            (float pointSide1X, float pointSide1Z) =
                ((float, float))MoreMath.AddVectorToPoint(_arrowHeadSideLength, yaw + 32768 + 8192, arrowHeadX, arrowHeadZ);
            (float pointSide2X, float pointSide2Z) =
                ((float, float))MoreMath.AddVectorToPoint(_arrowHeadSideLength, yaw + 32768 - 8192, arrowHeadX, arrowHeadZ);

            List<(float x, float y, float z)> vertices = new List<(float x, float y, float z)>();

            vertices.Add((x, y, z));
            vertices.Add((arrowHeadX, y, arrowHeadZ));

            vertices.Add((arrowHeadX, y, arrowHeadZ));
            vertices.Add((pointSide1X, y, pointSide1Z));

            vertices.Add((arrowHeadX, y, arrowHeadZ));
            vertices.Add((pointSide2X, y, pointSide2Z));

            return vertices;
        }

        protected abstract double GetYaw();

        protected abstract double GetRecommendedSize();

        public override Image GetInternalImage()
        {
            return Config.ObjectAssociations.ArrowImage;
        }

        public override ContextMenuStrip GetContextMenuStrip()
        {
            if (_contextMenuStrip == null)
            {
                _itemUseSpeedForArrowLength = new ToolStripMenuItem("Use Recommended Arrow Size");
                _itemUseSpeedForArrowLength.Click += (sender, e) =>
                {
                    MapObjectSettings settings = new MapObjectSettings(
                        arrowChangeUseRecommendedLength: true,
                        arrowNewUseRecommendedLength: !_useRecommendedArrowLength);
                    GetParentMapTracker().ApplySettings(settings);
                };
                _itemUseSpeedForArrowLength.Checked = _useRecommendedArrowLength;

                string suffix1 = string.Format(" ({0})", _arrowHeadSideLength);
                _itemSetArrowHeadSideLength = new ToolStripMenuItem(SET_ARROW_HEAD_SIDE_LENGTH_TEXT + suffix1);
                _itemSetArrowHeadSideLength.Click += (sender, e) =>
                {
                    string text = DialogUtilities.GetStringFromDialog(labelText: "Enter the side length of the arrow head:");
                    float? arrowHeadSideLength = ParsingUtilities.ParseFloatNullable(text);
                    if (!arrowHeadSideLength.HasValue) return;
                    MapObjectSettings settings = new MapObjectSettings(
                        arrowChangeHeadSideLength: true, arrowNewHeadSideLength: arrowHeadSideLength.Value);
                    GetParentMapTracker().ApplySettings(settings);
                };

                string suffix2 = string.Format(" ({0})", _angleOffset);
                _itemSetAngleOffset = new ToolStripMenuItem(SET_ANGLE_OFFSET_TEXT + suffix2);
                _itemSetAngleOffset.Click += (sender, e) =>
                {
                    string text = DialogUtilities.GetStringFromDialog(labelText: "Enter the angle offset:");
                    float? angleOffsetNullable = ParsingUtilities.ParseFloatNullable(text);
                    if (!angleOffsetNullable.HasValue) return;
                    MapObjectSettings settings = new MapObjectSettings(
                        arrowChangeAngleOffset: true, arrowNewAngleOffset: angleOffsetNullable.Value);
                    GetParentMapTracker().ApplySettings(settings);
                };

                _contextMenuStrip = new ContextMenuStrip();
                _contextMenuStrip.Items.Add(_itemUseSpeedForArrowLength);
                _contextMenuStrip.Items.Add(_itemSetArrowHeadSideLength);
                _contextMenuStrip.Items.Add(_itemSetAngleOffset);
            }

            return _contextMenuStrip;
        }

        public override void ApplySettings(MapObjectSettings settings)
        {
            base.ApplySettings(settings);

            if (settings.ArrowChangeUseRecommendedLength)
            {
                _useRecommendedArrowLength = settings.ArrowNewUseRecommendedLength;
                _itemUseSpeedForArrowLength.Checked = _useRecommendedArrowLength;
            }

            if (settings.ArrowChangeHeadSideLength)
            {
                _arrowHeadSideLength = settings.ArrowNewHeadSideLength;
                string suffix = string.Format(" ({0})", _arrowHeadSideLength);
                _itemSetArrowHeadSideLength.Text = SET_ARROW_HEAD_SIDE_LENGTH_TEXT + suffix;
            }

            if (settings.ArrowChangeAngleOffset)
            {
                _angleOffset = settings.ArrowNewAngleOffset;
                string suffix = string.Format(" ({0})", _angleOffset);
                _itemSetAngleOffset.Text = SET_ANGLE_OFFSET_TEXT + suffix;
            }
        }
    }
}
