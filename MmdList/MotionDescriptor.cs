using System.IO;
using System.Text;

namespace MmdList
{
    public class MotionDescriptor
    {
        public int[] Motion { get; } = new int[4];
        public string FileName { get; set; }

        public MotionDescriptor(string vmdPath)
        {
            FileName = Path.GetFileName(vmdPath);

            using FileStream stream = File.OpenRead(vmdPath);
            using BinaryReader reader = new(stream);

            string vmdVersion = Encoding.ASCII.GetString(reader.ReadBytes(30));

            bool version2 = vmdVersion.StartsWith("Vocaloid Motion Data 0002");

            string name = Encoding.ASCII.GetString(reader.ReadBytes(version2 ? 20 : 10));

            if (ReadByte(0, 111)) return;
            if (ReadByte(1, 23)) return;
            if (ReadByte(2, 61)) return;
            if (ReadByte(3, 28)) return;
            return;

            bool ReadByte(int index, int length)
            {
                if (reader.BaseStream.Length <= reader.BaseStream.Position)
                    return true;

                Motion[index] = (int)reader.ReadUInt32();
                if (Motion[index] > 0)
                {
                    if (Motion[index] * length <= reader.BaseStream.Length - reader.BaseStream.Position)
                        reader.BaseStream.Seek(Motion[index] * length, SeekOrigin.Current);
                }

                return false;
            }
        }
    }
}