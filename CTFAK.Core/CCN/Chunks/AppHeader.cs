﻿using CTFAK.Memory;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CTFAK.Utils;

namespace CTFAK.CCN.Chunks
{
    public class AppHeader : ChunkLoader
    {
        public int Size;
        public int WindowWidth;
        public int WindowHeight;
        public int InitialScore;
        public int InitialLives;
        public int NumberOfFrames;
        public BitDict Flags = new BitDict(new string[]
        {
            "BorderMax",
            "NoHeading",
            "Panic",
            "SpeedIndependent",
            "Stretch",
            "MusicOn",
            "SoundOn",
            "MenuHidden",
            "MenuBar",
            "Maximize",
            "MultiSamples",
            "FullscreenAtStart",
            "FullscreenSwitch",
            "Protected",
            "Copyright",
            "OneFile"
        });
        public BitDict NewFlags = new BitDict(new string[]
        {
            "SamplesOverFrames",
            "RelocFiles",
            "RunFrame",
            "SamplesWhenNotFocused",
            "NoMinimizeBox",
            "NoMaximizeBox",
            "NoThickFrame",
            "DoNotCenterFrame",
            "ScreensaverAutostop",
            "DisableClose",
            "HiddenAtStart",
            "XPVisualThemes",
            "VSync",
            "RunWhenMinimized",
            "MDI",
            "RunWhileResizing"
        });

        public Color BorderColor;
        public int FrameRate;
        public short GraphicsMode;
        public short Otherflags;
        public Controls Controls;
        public int WindowsMenuIndex;
        public AppHeader(ByteReader reader) : base(reader) { }
        public override void Read()
        {
            var start = reader.Tell();
            if(!Settings.Old) Size = reader.ReadInt32();
            Flags.flag = (uint)reader.ReadInt16();

            NewFlags.flag = (uint)reader.ReadInt16();
            GraphicsMode = reader.ReadInt16();
            Otherflags = reader.ReadInt16();
            WindowWidth = reader.ReadInt16();
            WindowHeight = reader.ReadInt16();
            InitialScore = (int)(reader.ReadUInt32() ^ 0xffffffff);
            InitialLives = (int)(reader.ReadUInt32() ^ 0xffffffff);
            Controls = new Controls(reader);
            if(Settings.Old) reader.Skip(56);
            else Controls.Read();
            BorderColor = reader.ReadColor();
            NumberOfFrames = reader.ReadInt32();
            if (Settings.Old) return;
            FrameRate = reader.ReadInt32();
            WindowsMenuIndex = reader.ReadInt32();
        }

        public override void Write(ByteWriter writer)
        {
            throw new NotImplementedException();
        }
    }
    public class Controls : ChunkLoader
    {
        public List<PlayerControl> Items;

        public Controls(ByteReader reader) : base(reader) { }




        public override void Read()
        {
            Items = new List<PlayerControl>();
            for (int i = 0; i < 4; i++)
            {
                var item = new PlayerControl(reader);
                Items.Add(item);
                item.Read();
            }
        }

        public override void Write(ByteWriter Writer)
        {
            foreach (PlayerControl control in Items)
            {
                control.Write(Writer);
            }
        }


    }

    public class PlayerControl
    {
        int _controlType;
        ByteReader _reader;
        Keys _keys;

        public PlayerControl(ByteReader reader)
        {
            this._reader = reader;
        }

        public void Read()
        {
            _keys = new Keys(_reader);
            _controlType = _reader.ReadInt16();
            _keys.Read();
        }

        public void Write(ByteWriter Writer)
        {
            Writer.WriteInt16((short)_controlType);
            _keys.Write(Writer);

        }


    }

    public class Keys
    {
        short _up;
        short _down;
        short _left;
        short _right;
        short _button1;
        short _button2;
        short _button3;
        short _button4;
        ByteReader _reader;

        public Keys(ByteReader reader)
        {
            this._reader = reader;
        }


        public void Read()
        {
            _up = _reader.ReadInt16();
            _down = _reader.ReadInt16();
            _left = _reader.ReadInt16();
            _right = _reader.ReadInt16();
            _button1 = _reader.ReadInt16();
            _button2 = _reader.ReadInt16();
            //if (Settings.GameType == GameType.OnePointFive) return;
            _button3 = _reader.ReadInt16();
            _button4 = _reader.ReadInt16();
        }

        public void Write(ByteWriter Writer)
        {
            Writer.WriteInt16(_up);
            Writer.WriteInt16(_down);
            Writer.WriteInt16(_left);
            Writer.WriteInt16(_right);
            Writer.WriteInt16(_button1);
            Writer.WriteInt16(_button2);
            Writer.WriteInt16(_button3);
            Writer.WriteInt16(_button4);

        }


    }
}
