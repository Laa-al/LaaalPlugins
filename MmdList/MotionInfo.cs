using System;
using System.IO;
using System.Text;

namespace MmdList
{
    public class MotionInfo
    {
        public int Motion { get; }
        public int Morph { get; }
        public int Camera { get; }
        public int Light { get; }
        public string FileName { get; set; }

        private MotionInfo(string fileName,
            int motion, int morph, int camera, int light)
        {
            FileName = fileName;
            Motion = motion;
            Morph = morph;
            Camera = camera;
            Light = light;
        }

        public static MotionInfo Create(string filePath)
        {
            try
            {
                string fileName = Path.GetFileName(filePath);

                using FileStream stream = File.OpenRead(filePath);
                using BinaryReader reader = new(stream);

                string vmdVersion = Encoding.ASCII.GetString(reader.ReadBytes(30));

                bool version2 = vmdVersion.StartsWith("Vocaloid Motion Data 0002");

                string name = Encoding.ASCII.GetString(reader.ReadBytes(version2 ? 20 : 10));

                return new MotionInfo(fileName,
                    ReadByte(111), ReadByte(23),
                    ReadByte(61), ReadByte(28)
                );

                int ReadByte(int length)
                {
                    if (reader.BaseStream.Length <= reader.BaseStream.Position)
                    {
                        return 0;
                    }

                    int ret = (int)reader.ReadUInt32();
                    if (ret > 0
                        && ret * length <= reader.BaseStream.Length - reader.BaseStream.Position)
                    {
                        reader.BaseStream.Seek(ret * length, SeekOrigin.Current);
                    }

                    return ret;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
        }
    }
}