﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using PanicAttack;

namespace Mackiloha.Milo
{
    public partial class MiloFile
    {
        private static byte[] ADDE_PADDING = { 0xAD, 0xDE, 0xAD, 0xDE }; // Used to pad files

        private static MiloFile ParseDirectory(AwesomeReader ar, BlockStructure structure, uint offset)
        {
            bool origBigEndian = ar.BigEndian; // Used to preserve orig stream
            MiloFile milo;
            MiloVersion version;
            bool valid;
            string dirName, dirType;
            string[] entryNames, entryTypes;

            // Guesses endianess
            ar.BigEndian = DetermineEndianess(ar.ReadBytes(4), out version, out valid);
            if (!valid) return null; // Maybe do something else later

            ParseEntryNames(ar, version, out dirName, out dirType, out entryNames, out entryTypes);
            milo = new MiloFile(dirName, dirType, ar.BigEndian);
            milo._structure = structure;
            milo._offset = offset;
            milo._version = version;

            // TODO: Add component parser (Difficult)

            // Reads each file
            for (int i = 0; i < entryNames.Length; i++)
            {
                long start = ar.BaseStream.Position;
                int size = (int)(ar.FindNext(ADDE_PADDING));

                ar.BaseStream.Position = start;
                milo.Entries.Add(new MiloEntry(entryNames[i], entryTypes[i], ar.ReadBytes(size), milo.BigEndian));
                ar.BaseStream.Position += 4; // Jumps ADDE padding

                /* TODO: Implement milo files as entries
                if (type[i] == "ObjectDir" || type[i] == "MoveDir")
                {
                    // Directory embedded as an entry
                    // Skips over redundant directory info
                    ar.BaseStream.Position += 4;
                    dir.Entries.Add(MiloFile.FromStream(ar));
                }
                else
                {
                    // Regular entry
                    ar.BaseStream.Position = start;
                    dir.Entries.Add(new MEntry(name[i], type[i], ar.ReadBytes(size)));
                    ar.BaseStream.Position += 4;
                } */
            }

            ar.BigEndian = origBigEndian;
            return milo;
        }

        private static void ParseEntryNames(AwesomeReader ar, MiloVersion version, out string dirName, out string dirType, out string[] names, out string[] types)
        {
            dirName = dirType = ""; // Only used on versions 24+
            int count;
            
            if ((int)version >= 24)
            {
                // Parse directory name + type
                dirType = ar.ReadString();
                dirName = ar.ReadString();
                ar.BaseStream.Position += 8; // Skips weird counts
            }

            count = ar.ReadInt32();
            names = new string[count];
            types = new string[count];

            for (int i = 0; i < count; i++)
            {
                // Reads entry name + type
                types[i] = ar.ReadString();
                names[i] = ar.ReadString();
            }
        }

        private static bool DetermineEndianess(byte[] head, out MiloVersion version, out bool valid)
        {
            bool bigEndian = false;
            version = (MiloVersion)BitConverter.ToInt32(head, 0);
            valid = IsVersionValid(version);

            checkVersion:
            if (!valid && !bigEndian)
            {
                bigEndian = !bigEndian;
                Array.Reverse(head);
                version = (MiloVersion)BitConverter.ToInt32(head, 0);
                valid = IsVersionValid(version);

                goto checkVersion;
            }
            
            return bigEndian;
        }

        private static bool IsVersionValid(MiloVersion version)
        {
            switch (version)
            {
                case MiloVersion.V6:  // FreQ
                case MiloVersion.V10: // Amp/KR/GH1
                case MiloVersion.V24: // GH2(PS2)
                case MiloVersion.V25: // RB/GH2(X360)
                case MiloVersion.V28: // RB3
                case MiloVersion.V32: // RBB/DC3
                    return true;
                default:
                    return false;
            }
        }
    }
}
