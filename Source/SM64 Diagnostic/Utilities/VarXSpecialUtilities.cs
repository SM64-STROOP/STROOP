﻿using SM64_Diagnostic.Managers;
using SM64_Diagnostic.Structs.Configurations;
using SM64_Diagnostic.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SM64_Diagnostic.Structs
{
    public static class VarXSpecialUtilities
    {
        public readonly static Func<List<object>> DEFAULT_GETTER = () => new List<object>() { "UNIMPLEMENTED" };
        public readonly static Action<string> DEFAULT_SETTER = (string stringValue) => { };

        public static (Func<List<object>> getter, Action<string> setter) CreateGetterSetterFunctions(string specialType)
        {
            Func<List<object>> getterFunction = DEFAULT_GETTER;
            Action<string> setterFunction = DEFAULT_SETTER;

            switch (specialType)
            {
                case "MarioDistanceToObject":
                    getterFunction = () =>
                    {
                        Position marioPos = GetMarioPosition();
                        List<Position> objPoses = GetObjectPositions();
                        return objPoses.ConvertAll(objPos =>
                        {
                            return (object)MoreMath.GetDistanceBetween(
                                marioPos.X, marioPos.Y, marioPos.Z, objPos.X, objPos.Y, objPos.Z);
                        });
                    };
                    break;

                case "MarioHorizontalDistanceToObject":
                    break;

                case "MarioVerticalDistanceToObject":
                    break;

                case "RngIndex":
                    getterFunction = () =>
                    {
                        ushort rngValue = Config.Stream.GetUInt16(Config.RngAddress);
                        string rngIndexString = RngIndexer.GetRngIndexString(rngValue);
                        return CreateList(rngIndexString);
                    };
                    setterFunction = (string stringValue) =>
                    {
                        int? index = ParsingUtilities.ParseIntNullable(stringValue);
                        if (index.HasValue)
                        {
                            ushort rngValue = RngIndexer.GetRngValue(index.Value);
                            Config.Stream.SetValue(rngValue, Config.RngAddress);
                        }
                    };
                    break;

                default:
                    break;
            }

            return (getterFunction, setterFunction);
        }

        private static List<object> CreateList(params object[] objs)
        {
            return new List<object>(objs);
        }

        private static Position GetMarioPosition()
        {
            float marioX = Config.Stream.GetSingle(Config.Mario.StructAddress + Config.Mario.XOffset);
            float marioY = Config.Stream.GetSingle(Config.Mario.StructAddress + Config.Mario.YOffset);
            float marioZ = Config.Stream.GetSingle(Config.Mario.StructAddress + Config.Mario.ZOffset);
            return new Position(marioX, marioY, marioZ);
        }

        private static List<Position> GetObjectPositions()
        {
            return ObjectManager.Instance.CurrentAddresses.ConvertAll(objAddress =>
            {
                float objX = Config.Stream.GetSingle(objAddress + Config.ObjectSlots.ObjectXOffset);
                float objY = Config.Stream.GetSingle(objAddress + Config.ObjectSlots.ObjectYOffset);
                float objZ = Config.Stream.GetSingle(objAddress + Config.ObjectSlots.ObjectZOffset);
                return new Position(objX, objY, objZ);
            });
        }

        private struct Position
        {
            public readonly float X;
            public readonly float Y;
            public readonly float Z;

            public Position(float x, float y, float z)
            {
                X = x;
                Y = y;
                Z = z;
            }
        }
    }
}