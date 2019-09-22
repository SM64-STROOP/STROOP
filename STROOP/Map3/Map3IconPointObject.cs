﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using STROOP.Controls.Map;
using OpenTK.Graphics.OpenGL;
using STROOP.Utilities;
using STROOP.Structs.Configurations;
using STROOP.Structs;
using OpenTK;

namespace STROOP.Map3
{
    public abstract class Map3IconPointObject : Map3IconObject
    {
        public Map3IconPointObject(Func<Image> imageFunction)
            : base(imageFunction)
        {
        }

        public override void DrawOnControl()
        {
            UpdateImage();

            // Update map object
            (double x, double y, double z, double angle) = GetPositionAngle();
            if (double.IsNaN(angle)) angle = 0;
            float relX = (float)PuUtilities.GetRelativeCoordinate(x);
            float relY = (float)PuUtilities.GetRelativeCoordinate(y);
            float relZ = (float)PuUtilities.GetRelativeCoordinate(z);
            float angleDegrees = (float)MoreMath.AngleUnitsToDegrees(angle);

            float xOffsetInGameUnits = relX - Config.Map3Graphics.XMin;
            float xOffsetPixels = xOffsetInGameUnits * Config.Map3Graphics.ConversionScale;
            float xPosPixels = Config.Map3Graphics.MapView.X + xOffsetPixels;

            float zOffsetInGameUnits = relZ - Config.Map3Graphics.ZMin;
            float zOffsetPixels = zOffsetInGameUnits * Config.Map3Graphics.ConversionScale;
            float zPosPixels = Config.Map3Graphics.MapView.Y + zOffsetPixels;

            SizeF size = ScaleImageSize(Image.Size, Size);

            DrawTexture(new PointF(xPosPixels, zPosPixels), size, angleDegrees);
        }

        private static SizeF ScaleImageSize(Size imageSize, float desiredSize)
        {
            float scale = Math.Max(imageSize.Height / desiredSize, imageSize.Width / desiredSize);
            return new SizeF(imageSize.Width / scale, imageSize.Height / scale);
        }

        protected abstract (double x, double y, double z, double angle) GetPositionAngle();
    }
}