using System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameFactoryImgExtract
{
    internal class EncodedImage : IDisposable
    {
        private struct Pointer
        {
            public uint colorOpaquenessLine = 0;
            public uint colorListLine = 0;

            public Pointer()
            {
            }
        }
        

        public struct ImageHeaderType0
        {
            public ushort unknown1 = 0;
            public ushort unknown2 = 0;
            public ushort unused = 0;
            public uint fileLength = 0;
            public ushort sizeX = 0;
            public ushort sizeY = 0;
            public byte colorMode = 0;
            public byte compression = 0;
            public ushort hotSpotX = 0;
            public ushort hotSpotY = 0;
            public ushort actionPointX = 0;
            public ushort actionPointY = 0;

            public int Bit_Count
            {
                get
                {
                    return colorMode switch
                    {
                        0x3 => 8,
                        0x4 => 24,
                        0x6 => 15,
                        0x7 => 16,
                        _ => 24,
                    };
                }
            }
            public bool CompressionRLE
            {
                get
                {
                    return (compression & 0xF) != 0;
                }
            }
            public bool CompressionUnused1
            {
                get
                {
                    return (compression & 0x20) != 0;
                }
            }
            public bool CompressionTGF
            {
                get
                {
                    return (compression & 0x40) != 0;
                }
            }
            public bool CompressionUnused2
            {
                get
                {
                    return (compression & 0x80) != 0;
                }
            }

            public ImageHeaderType0()
            {
            }
        }
        public struct ImageInfo
        {
            private uint _filePos = 0;
            private uint _fileLen = 0;

            public uint FilePos
            {
                get
                {
                    return _filePos;
                }
                set
                {
                    _filePos = value;
                }
            }

            public uint FileLen
            {
                get
                {
                    return _fileLen;
                }
                set
                {
                    _fileLen = value;
                }
            }

            public ImageInfo()
            {
            }
        }

        private ImageInfo _info;
        private ImageHeaderType0 _header;


        private readonly ImgFile _container;
        private byte[] _compressedImageData = Array.Empty<byte>();

        private bool _isLoaded = false;
        public bool IsLoaded { get { return _isLoaded; } }

        private bool[,] _opaquenessData = new bool[0, 0];
        private Color[,] _colorData = new Color[0, 0];

        public Size Size
        {
            get
            {
                return new Size(_header.sizeX, _header.sizeY);
            }
        }
        public ImageHeaderType0 Header
        {
            get
            {
                return _header;
            }
        }
        public ImageInfo CreationInfo
        {
            get
            {
                return _info;
            }
        }
        
        public EncodedImage(ImageInfo info, ImgFile container)
        {
            this._info = info;
            this._container = container;
        }

        public void Load()
        {
            GetHeader(_info, _container.Data, ref _header);
            _compressedImageData = new byte[_header.fileLength];
            Array.Copy(_container.Data, _info.FilePos + 24, _compressedImageData, 0, _header.fileLength);
            _opaquenessData = new bool[_header.sizeX, _header.sizeY];
            _colorData = new Color[_header.sizeX, _header.sizeY];

            _isLoaded = true;
        }

        public (Image img, Exception? ex) GetImage()
        {
            Exception? err = null;
            try
            {
                if (_header.CompressionTGF)
                    DecompressTGF();
                else if (_header.CompressionRLE)
                    DecompressRLE();
            }
            catch (Exception ex)
            {
                err = ex;
            }

            Bitmap bmp = new(_header.sizeX, _header.sizeY);

            for (int x = 0; x < _header.sizeX; x++)
            {
                for (int y = 0; y < _header.sizeY; y++)
                {
                    Color color = Color.FromArgb(_opaquenessData[x, y] ? 255 : 0, _colorData[x, y].R, _colorData[x, y].G, _colorData[x, y].B);
                    bmp.SetPixel(x, y, color);
                }
            }

            return (bmp, err);
        }

        private void DecompressTGF()
        {
            uint pos = 0;
            //uint bufferSize = Helper.LittleEndianCopy32(_compressedImageData, pos, 4); //Not used lol
            uint pointer;
            pos += 4;
            Pointer[] pointerList = new Pointer[_header.sizeY];
            for (int i = 0; i < _header.sizeY; i++)
            {
                pointerList[i].colorOpaquenessLine = Helper.LittleEndianCopy32(_compressedImageData, pos, 4);
                pos += 4;
                pointerList[i].colorListLine = Helper.LittleEndianCopy32(_compressedImageData, pos, 4);
                pos += 4;
            }
            uint colorPointer = pointerList[0].colorListLine + 4;
            uint colorRepeat = 0;
            bool colorMode = false;
            int tempColor = 0x80;
            Color color = Color.FromArgb(0, 0, 0);
            for (int y = 0; y < _header.sizeY; y++)
            {
                pointer = pointerList[y].colorOpaquenessLine + 4;
                uint x = 0;
                do
                {
                    //transparent
                    byte transparent = _compressedImageData[pointer];
                    byte opaque = _compressedImageData[pointer + 1];
                    pointer += 2;
                    for (int i = 0; i < transparent; i++)
                    {
                        _opaquenessData[x + i, y] = false;
                    }
                    x += transparent;
                    if (x >= _header.sizeX) break;
                    //opaque
                    for (int i = 0; i < opaque; i++)
                    {
                        if (colorRepeat == 0)
                        {
                            byte colorModeInfo = _compressedImageData[colorPointer];
                            colorPointer++;
                            colorRepeat = (byte)(colorModeInfo & 0x7F);
                            colorMode = (colorModeInfo & 0x80) == 0x80;
                            if (!colorMode)
                            {
                                color = GetColor(_compressedImageData, ref colorPointer);
                            }
                        }
                        _opaquenessData[x + i, y] = true;
                        if (colorMode)
                        {
                            _colorData[x + i, y] = GetColor(_compressedImageData, ref colorPointer);
                            colorRepeat--;
                        }
                        else
                        {
                            _colorData[x + i, y] = color;
                            colorRepeat--;
                        }


                        while ((tempColor & 0x7F) == 0x0)
                        {
                            if ((tempColor & 0x80) == 0)
                            {

                            }
                            if (!Header.CompressionRLE)
                            {
                                tempColor = _compressedImageData[colorPointer];
                                colorPointer--;
                            }
                            else
                            {
                                tempColor = 0x81;
                            }
                        }


                        tempColor--;
                    }
                    x += opaque;
                } while (x < _header.sizeX);
            }
        }

        private Color GetColor(byte[] data, ref uint pointer)
        {
            byte color1;
            byte color2;
            byte color3;
            switch (_header.Bit_Count)
            {
                case 8:
                    color1 = data[pointer];
                    pointer++;
                    return _container.palette.GetColorAt(color1);
                case 15:
                case 16:
                    color1 = data[pointer];
                    color2 = data[pointer + 1];
                    pointer += 2;
                    return Color.FromArgb(color1, color2, color1);
                case 24:
                default:
                    color1 = data[pointer];
                    color2 = data[pointer + 1];
                    color3 = data[pointer + 2];
                    pointer += 3;
                    return Color.FromArgb(color1, color2, color3);
            }
        }

        private void DecompressRLE()
        {
            throw new NotImplementedException();
        }

        static void GetHeader(ImageInfo info, byte[] input, ref ImageHeaderType0 header)
        {
            header = new ImageHeaderType0();

            Helper.LittleEndianCopy16( input, info.FilePos, 2, ref header.unknown1);
            Helper.LittleEndianCopy16( input, info.FilePos + 2, 2, ref header.unknown2);
            Helper.LittleEndianCopy16( input, info.FilePos + 4, 2, ref header.unused);
            Helper.LittleEndianCopy32( input, info.FilePos + 6, 4, ref header.fileLength);
            Helper.LittleEndianCopy16( input, info.FilePos + 10, 2, ref header.sizeX);
            Helper.LittleEndianCopy16( input, info.FilePos + 12, 2, ref header.sizeY);
            header.colorMode = input[info.FilePos + 14];
            header.compression = input[info.FilePos + 15];
            Helper.LittleEndianCopy16( input, info.FilePos + 16, 2, ref header.hotSpotX);
            Helper.LittleEndianCopy16( input, info.FilePos + 18, 2, ref header.hotSpotY);
            Helper.LittleEndianCopy16( input, info.FilePos + 18, 2, ref header.actionPointX);
            Helper.LittleEndianCopy16( input, info.FilePos + 18, 2, ref header.actionPointY);
        }

        public void Dispose()
        {
        }
    }
}
