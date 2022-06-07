namespace GameFactoryImgExtract
{
    internal class ImgFile
    {
        private readonly string _fileName;
        private byte[] _data = Array.Empty<byte>();

        private uint _resImgCount;

        public Palette palette = new();

        public byte[] Data
        {
            get { return _data; }
        }

        private readonly List<EncodedImage> _images = new();

        public uint ImageCount
        {
            get { return _resImgCount; }
        }
        public string FileName
        {
            get { return _fileName; }
        }

        public EncodedImage GetImage(int index)
        {
            if (index < 0 || index >= _resImgCount)
                throw new IndexOutOfRangeException();

            if (_images[index].CreationInfo.FileLen == 0)
            {
                return _images[index];
            }

            if (!_images[index].IsLoaded)
                _images[index].Load();

            return _images[index];
        }

        public ImgFile(string fileName)
        {
            this._fileName = fileName;
        }

        public void Load()
        {
            this._data = File.ReadAllBytes(this._fileName);
            string headSig = Helper.GetAsString(this._data, 0, 4);

            int FileFmt = headSig switch
            {
                "AGMI" => 10,
                "APMS" or "ASUM" or "ATNF" => throw new NotImplementedException("Wrong Format"),
                _ => 0,
            };

            EncodedImage.ImageInfo[] imageInfos;

            switch (FileFmt)
            {
                case 0x0:
                    {
                        Helper.LittleEndianCopy32(this._data, 0, 4, ref this._resImgCount);
                        imageInfos = new EncodedImage.ImageInfo[this._resImgCount];
                        CopyImgInfo(this._data, 4, _resImgCount, ref imageInfos);
                        break;
                    }
                case 0x10:
                case 0x20:
                    {
                        //TODO: Implement
                        throw new NotImplementedException();
                    }
                default:
                    throw new Exception("Unknown File Format");
            }

            _images.Clear();

            foreach (EncodedImage.ImageInfo imageInfo in imageInfos)
            {
                _images.Add(new EncodedImage(imageInfo, this));
            }
        }

        public static void CopyImgInfo(byte[] inputData, uint startIndex, uint length, ref EncodedImage.ImageInfo[] output)
        {
            for (int i = 0; i < length; i++)
            {
                uint filePos = 0;
                uint fileLen = 0;
                Helper.LittleEndianCopy32(inputData, (uint)(startIndex + i * 8), 4, ref filePos);
                Helper.LittleEndianCopy32(inputData, (uint)(startIndex + 4 + i * 8), 4, ref fileLen);

                output[i] = new()
                {
                    FilePos = filePos,
                    FileLen = fileLen,
                };
            }
        }
    }
}
