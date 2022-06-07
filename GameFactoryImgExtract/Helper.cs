using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameFactoryImgExtract
{
    internal static class Helper
    {
        public static void GetAsString(byte[] inputData, int start, int length, ref string result)
        {
            result = "";

            for (int i = start; i < start + length; i++)
            {
                result += (char)(inputData[i]);
            }
        }

        public static string GetAsString(byte[] inputData, int start, int length)
        {
            string result = "";

            for (int i = start; i < start + length; i++)
            {
                result += (char)(inputData[i]);
            }

            return result;
        }

        public static void LittleEndianCopy32(byte[] inputData, uint index, uint length, ref uint output)
        {
            output = 0;
            for (int i = 0; i < length; i++)
            {
                output |= (uint)(inputData[index + i] << (i * 8));
            }
        }

        public static uint LittleEndianCopy32(byte[] inputData, uint index, uint length)
        {
            uint output = 0;
            for (int i = 0; i < length; i++)
            {
                output |= (uint)(inputData[index + i] << (i * 8));
            }
            return output;
        }

        public static void LittleEndianCopy16(byte[] inputData, uint index, uint length, ref ushort output)
        {
            output = 0;
            for (int i = 0; i < length; i++)
            {
                output |= (ushort)(inputData[index + i] << (i * 8));
            }
        }

        public static ushort LittleEndianCopy16(byte[] inputData, uint index, uint length)
        {
            ushort output = 0;
            for (int i = 0; i < length; i++)
            {
                output |= (ushort)(inputData[index + i] << (i * 8));
            }
            return output;
        }
    }
}
