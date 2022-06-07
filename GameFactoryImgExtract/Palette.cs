using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameFactoryImgExtract
{
    internal class Palette
    {

        private Color[] _colorList = new Color[256];

        public Color GetColorAt(int index)
        {
            if (index >= _colorList.Length) throw new IndexOutOfRangeException();
            return _colorList[index];
        }


        public Palette()
        {
            Random random = new();
            for (int i = 0; i < 256; i++)
            {
                _colorList[i] = RandomColor(random);
            }
        }

        public Palette(string file)
        {
            Load(file);
        }

        public void Load(string file)
        {
            byte[] data = File.ReadAllBytes(file);
            _colorList = new Color[(data.Length / 4)-1];
            for (int i = 0; i < data.Length/4-4; i++)
            {
                _colorList[i] = Color.FromArgb(data[i * 4 + 3], data[i * 4 + 4], data[i * 4 + 5], data[i * 4 + 6]);
            }
            ;
        }

        private static Color RandomColor(Random random)
        {
            byte R = (byte)random.Next(0, 256);
            byte G = (byte)random.Next(0, 256);
            byte B = (byte)random.Next(0, 256);
            return Color.FromArgb(R, G, B);
        }
    }
}
