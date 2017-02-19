using System;
using System.IO;
using System.Collections.Generic;

class RiffMaster
{
	public RiffMaster() { }

	public void WriteFile(string fname)
	{
		using (FileStream fs = new FileStream(fname,FileMode.Create,FileAccess.Write,FileShare.Read))
			WriteStream(fs);
	}

	public void LoadFile(string fname)
	{
		using (FileStream fs = File.OpenRead(fname))
			LoadStream(fs);
	}

	private static string ReadTag(BinaryReader br)
	{
		return "" + br.ReadChar() + br.ReadChar() + br.ReadChar() + br.ReadChar();
	}

	protected static void WriteTag(BinaryWriter bw, string tag)
	{
		for(int i=0;i<4;i++)
			bw.Write(tag[i]);
		bw.Flush();
	}

	public abstract class RiffChunk
	{
		public string tag;
		public abstract void WriteStream(Stream s);
		public abstract long GetSize();
		public abstract RiffChunk Morph();
	}

	public class RiffSubchunk : RiffChunk
	{
		public byte[] data = new byte[0];
		public override void WriteStream(Stream s)
		{
			BinaryWriter bw = new BinaryWriter(s);
			WriteTag(bw,tag);
			bw.Write(data.Length);
			bw.Write(data);
			bw.Flush();
			if (data.Length % 2 != 0)
				s.WriteByte(0);
		}
		public override long GetSize()
		{
			long ret = data.Length;
			if (ret % 2 != 0) ret++;
			return ret;
		}

		public override RiffChunk Morph()
		{
			switch (tag)
			{
				case "fmt ": return new RiffSubchunk_fmt(data);
				case "smpl": return new RiffSubchunk_smpl(data);
			}
			return this;
		}
	}


	public class RiffSubchunk_smpl : RiffSubchunk
	{
		public RiffSubchunk_smpl(byte[] data)
		{
			this.data = data;
			tag = "smpl";
			BinaryReader br = new BinaryReader(new MemoryStream(data));
			br.BaseStream.Position += 7 * 4; //skip unusued fields
			int numLoops = br.ReadInt32();
			br.BaseStream.Position += 4; //skip sampler data

			for (int i = 0; i < numLoops; i++)
			{
				var sl = new SampleLoop();
				sl.CuePointId = br.ReadInt32();
				sl.Type = br.ReadInt32();
				sl.Start = br.ReadInt32();
				sl.End = br.ReadInt32();
				sl.Fraction = br.ReadInt32();
				sl.PlayCount = br.ReadInt32();
				SampleLoops.Add(sl);
			}
		}

		public List<SampleLoop> SampleLoops = new List<SampleLoop>();

		public class SampleLoop
		{
			public int CuePointId, Type, Start, End, Fraction, PlayCount;
		}
	}


	public class RiffSubchunk_fmt : RiffSubchunk
	{
		public enum FORMAT_TAG : ushort
		{
			MS_ADPCM = 1,
			G711_alaw = 6,
			G711_ulaw = 7,
			IMA_ADPCM = 17,
			G723 = 20,
			GSM = 49,
			G721 = 64,
			MPEG = 80,
			EXPERIMENTAL = 0xFFFF,
		}
		public FORMAT_TAG format_tag;
		public ushort channels;
		public uint samplesPerSec;
		public uint avgBytesPerSec;
		public ushort blockAlign;
		public ushort bitsPerSample;
		public RiffSubchunk_fmt(byte[] data)
		{
			this.data = data;
			tag = "fmt ";
			BinaryReader br = new BinaryReader(new MemoryStream(data));
			format_tag = (FORMAT_TAG)br.ReadUInt16();
			channels = br.ReadUInt16();
			samplesPerSec = br.ReadUInt32();
			avgBytesPerSec = br.ReadUInt32();
			blockAlign = br.ReadUInt16();
			bitsPerSample = br.ReadUInt16();
		}
		public override void WriteStream(Stream s)
		{
			MemoryStream ms = new MemoryStream();
			BinaryWriter bw = new BinaryWriter(ms);
			bw.Write((ushort)format_tag);
			bw.Write(channels);
			bw.Write(samplesPerSec);
			bw.Write(avgBytesPerSec);
			bw.Write(blockAlign);
			bw.Write(bitsPerSample);
			bw.Flush();
			data = ms.ToArray();
			base.WriteStream(s);
		}
	}

	public class RiffContainer : RiffChunk
	{
		public RiffChunk GetSubchunk(string tag, string type)
		{
			foreach (RiffChunk rc in subchunks) 
				if (rc.tag == tag)
				{
					if(type == null) return rc;
					RiffContainer cont = rc as RiffContainer;
					if (cont != null && cont.type == type)
						return rc;
				}
			return null;
		}

		public RiffContainer()
		{
			tag = "LIST";
		}
		public string type;
		public List<RiffChunk> subchunks = new List<RiffChunk>();
		public override void WriteStream(Stream s)
		{
			BinaryWriter bw = new BinaryWriter(s);
			WriteTag(bw, tag);
			long size = GetSize();
			if (size > uint.MaxValue) throw new FormatException("File too big to write out");
			bw.Write((uint)size);
			WriteTag(bw, type);
			bw.Flush();
			foreach (RiffChunk rc in subchunks)
				rc.WriteStream(s);
			if (size % 2 != 0)
				s.WriteByte(0);
		}
		public override long GetSize()
		{
			long len = 4;
			foreach (RiffChunk rc in subchunks)
				len += rc.GetSize() + 8;
			return len;
		}

		public override RiffChunk Morph()
		{
			switch (type)
			{
				case "INFO": return new RiffContainer_INFO(this);
			}
			return this;
		}
	}

	public class RiffContainer_INFO : RiffContainer
	{
		public Dictionary<string, string> dictionary = new Dictionary<string,string>();
		public RiffContainer_INFO() { type = "INFO"; }
		public RiffContainer_INFO(RiffContainer rc)
		{
			subchunks = rc.subchunks;
			type = "INFO";
			foreach (RiffChunk chunk in subchunks)
			{
				RiffSubchunk rsc = chunk as RiffSubchunk;
				if (chunk == null)
					throw new FormatException("Invalid subchunk of INFO list");
				dictionary[rsc.tag] = System.Text.Encoding.ASCII.GetString(rsc.data);
			}
		}

		private void Flush()
		{
			subchunks.Clear();
			foreach (KeyValuePair<string, string> kvp in dictionary)
			{
				RiffSubchunk rs = new RiffSubchunk();
				rs.tag = kvp.Key;
				rs.data = System.Text.Encoding.ASCII.GetBytes(kvp.Value);
				subchunks.Add(rs);
			}
		}

		public override long GetSize()
		{
			Flush();
			return base.GetSize();
		}

		public override void WriteStream(Stream s)
		{
			Flush();
			base.WriteStream(s);
		}
	}

	public RiffContainer riff;

	private long readCounter;
	private RiffChunk ReadChunk(BinaryReader br)
	{
		RiffChunk ret;
		string tag = ReadTag(br); readCounter += 4;
		uint size = br.ReadUInt32(); readCounter += 4;
		if (size > int.MaxValue) 
			throw new FormatException("chunk too big");
		if (tag == "RIFF" || tag == "LIST")
		{
			RiffContainer rc = new RiffContainer();
			rc.tag = tag;
			rc.type = ReadTag(br); readCounter += 4;
			long readEnd = readCounter - 4 + size;
			while (readEnd > readCounter)
				rc.subchunks.Add(ReadChunk(br));
			ret = rc.Morph();
		}
		else
		{
			RiffSubchunk rsc = new RiffSubchunk();
			rsc.tag = tag;
			rsc.data = br.ReadBytes((int)size); readCounter += size;
			ret = rsc.Morph();
		}
		if (size % 2 != 0)
		{
			br.ReadByte(); 
			readCounter += 1;
		}
		return ret;
		
	}

	public void WriteStream(Stream s)
	{
		riff.WriteStream(s);
	}

	public void LoadStream(Stream s)
	{
		readCounter = 0;
		BinaryReader br = new BinaryReader(s);
		RiffChunk chunk = ReadChunk(br);
		if (chunk.tag != "RIFF") throw new FormatException("can't recognize riff chunk");
		riff = (RiffContainer)chunk;
	}


}