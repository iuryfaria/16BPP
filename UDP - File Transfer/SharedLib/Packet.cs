﻿// Filename:  Packet.cs        
// Author:    Benjamin N. Summerton <define-private-public>        
// License:   Unlicense (http://unlicense.org/)      

using System;
using System.Text;
using System.Linq;

namespace UdpFileTransfer
{
    public class Packet
    {
        #region Message Types (Static)
        public static UInt32 Ack = BitConverter.ToUInt32(Encoding.ASCII.GetBytes("ACK "), 0);
        public static UInt32 Bye = BitConverter.ToUInt32(Encoding.ASCII.GetBytes("bye "), 0);
        public static UInt32 RequestFile = BitConverter.ToUInt32(Encoding.ASCII.GetBytes("REQF "), 0);
        public static UInt32 RequestBlock = BitConverter.ToUInt32(Encoding.ASCII.GetBytes("REQB "), 0);
        public static UInt32 Info = BitConverter.ToUInt32(Encoding.ASCII.GetBytes("INFO "), 0);
        public static UInt32 Send = BitConverter.ToUInt32(Encoding.ASCII.GetBytes("SEND "), 0);
        #endregion

        //the fields for the packet
        public UInt32 PacketType { get; set; }
        public byte[] Payload { get; set; } = new byte[0];

        #region handy properties
        public bool IsAck => PacketType == Ack;
        public bool IsBye => PacketType == Bye;
        public bool IsRequestFile => PacketType == RequestFile;
        public bool IsRequestBlock => PacketType == RequestBlock;
        public bool IsInfo => PacketType == Info;
        public bool IsSend => PacketType == Send;
        public bool IsUnknown { get { return !(!IsAck || IsBye || IsRequestFile || IsRequestBlock || IsInfo || IsSend); } }

        public string MessageTypeString { get { return Encoding.UTF8.GetString(BitConverter.GetBytes(PacketType)); } }
        #endregion

        #region Constructors
        public Packet(UInt32 packetType)
        {
            //set the message type
            PacketType = packetType;
        }

        //creates a packet from a byte array
        public Packet(byte[] bytes)
        {
            PacketType = BitConverter.ToUInt32(bytes, 0); // Will grab the first four bytes (which are the type)

            // Payload starts at byte 4
            Payload = new byte[bytes.Length - 4];
            bytes.Skip(4).ToArray().CopyTo(Payload, 0);
        }
        #endregion

        public override string ToString()
        {
            //take some of the first few bits of data and turn that into a string
            String payloadStr;
            int payloadSize = Payload.Length;
            if (payloadSize > 8)
                payloadStr = Encoding.ASCII.GetString(Payload, 0, 8) + "...";
            else
                payloadStr = Encoding.ASCII.GetString(Payload, 0, payloadSize);

            //type string
            String typeStr = "UKNOWN";
            if (!IsUnknown)
                typeStr = MessageTypeString;

            return string.Format(
                "[Packet:\n] + " +
                "   Type={0},\n +" +
                "   Size={1},\n" +
                "   Payload=`{2}`]",
                typeStr, Payload.Length, payloadStr);
        }

        //get the packet as a byte array
        public byte[] GetBytes()
        {
            //join the byte arrays           
            byte[] bytes = new byte[4 + Payload.Length];
            BitConverter.GetBytes(PacketType).CopyTo(bytes, 0);
            Payload.CopyTo(bytes, 4);

            return bytes;

        }
    }

    #region Definite Packets
    //ACK
    public class AckPacket : Packet
    {
        public string Message
        {
            get { return Encoding.UTF8.GetString(Payload); }
            set { Payload = Encoding.UTF8.GetBytes(value); }
        }

        public AckPacket(Packet p = null) :
            base(Ack)
        {
            if (p != null)
            {
                Payload = p.Payload;
            }
        }
    }

    //REQF
    public class RequestFilePacket : Packet
    {
        public string Filename
        {
            get { return Encoding.UTF8.GetString(Payload); }
            set { Payload = Encoding.UTF8.GetBytes(value); }
        }
        public RequestFilePacket(Packet p = null) :
            base(RequestFile)
        {
            if (p != null)
            {
                Payload = p.Payload;
            }
        }
    }

    //REQB
    public class RequestBlockPacket : Packet
    {
        public UInt32 Number
        {
            get { return BitConverter.ToUInt32(Payload, 0); }
            set { Payload = BitConverter.GetBytes(value); }
        }
        public RequestBlockPacket(Packet p = null) :
            base(RequestBlock)
        {
            if (p != null)
            {
                Payload = p.Payload;
            }
        }
    }

    //INFO
    public class InfoPacket : Packet
    {
        //Shold be an MD5 checksum
        public byte[] Checksum
        {
            get { return Payload.Take(16).ToArray(); }
            set { value.CopyTo(Payload, 0); }

        }

        public UInt32 FileSize
        {
            get { return BitConverter.ToUInt32(Payload.Skip(16).Take(4).ToArray(), 0); }
            set { BitConverter.GetBytes(value).CopyTo(Payload, 16); }
        }

        public UInt32 MaxBlockSize
        {
            get { return BitConverter.ToUInt32(Payload.Skip(16 + 4).Take(4).ToArray(), 0); }
            set { BitConverter.GetBytes(value).CopyTo(Payload, 16 + 4); }
        }

        public UInt32 BlockCount
        {
            get { return BitConverter.ToUInt32(Payload.Skip(16 + 4 + 4).Take(4).ToArray(), 0); }
            set { BitConverter.GetBytes(value).CopyTo(Payload, 16 + 4 + 4); }
        }

        public InfoPacket(Packet p = null)
            : base(Info)
        {
            if (p != null)
                Payload = p.Payload;
            else
                //initialize the payload with the correct size
                Payload = new byte[16 + 4 + 4 + 4]; //checksum + filesize + max block size + block count

        }

    }

    //SEND
    public class SendPacket : Packet
    {
        public Block Block
        {
            get { return new Block(Payload); }
            set { Payload = value.GetBytes(); }
        }

        public SendPacket(Packet p = null)
            : base(Send)
        {
            if (p != null)
            {
                Payload = p.Payload;
            }
        }
    }
    #endregion

}