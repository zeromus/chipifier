using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace Chipifier
{
	public partial class Program
	{
		public Program()
		{
		}

		[STAThread]
		static void Main(string[] args)
		{
			new Program().RunImportJob(args);
		}

		void RunImportJob(IEnumerable<string> files)
		{
			foreach(var f in files)
			{
				RiffMaster rm = new RiffMaster();
				rm.LoadFile(f);
				var dataChunk = rm.riff.GetSubchunk("data", null) as RiffMaster.RiffSubchunk;
				var fmt = rm.riff.GetSubchunk("fmt ", null) as RiffMaster.RiffSubchunk_fmt;
				BinaryReader br = new BinaryReader(new MemoryStream(dataChunk.data));
				int nSamples = dataChunk.data.Length/fmt.blockAlign;
				int nChunks = nSamples / 32;

				if (fmt.channels == 2) throw new InvalidOperationException("expected mono file");

				br = new BinaryReader(new MemoryStream(dataChunk.data));
				var ms = new MemoryStream();
				BinaryWriter bw = new BinaryWriter(ms);
				for (int j = 0; j < nChunks; j++)
				{
					string outfile = f + ".bin." + j;
					StreamWriter sw = new StreamWriter(outfile);
					for (int i = 0; i < 32; i++)
					{
						short ssample = br.ReadInt16();
						int sample = ssample + 16;
						if(sample>31) sample=31; //clamp in case we overdrived or whatever
						if (sample < 0) throw new InvalidOperationException("oops minus 0");
						sw.Write("{0} ",sample);
					}
					sw.Close();
				}
			
				rm.WriteFile(f);
			}
		}
	}
}
