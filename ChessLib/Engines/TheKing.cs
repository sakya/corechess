using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.SymbolStore;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ChessLib.Engines
{
	/// <summary>
	/// Chessmaster chess engine (The King)
	/// </summary>
	public class TheKing : Cecp
	{
		#region classes
		public class Personality
		{
			#region constants
			/*************************************************
			* Chessmaster 9000, X, XI Personality file offsets
			**************************************************/
			private const int CM_PROPERTY_SIZE = 0x04; //Unless otherwise noted, all properties are ints (4 bytes

			private const int CM_HEADER_OFFSET = 0;
			private const int CM_HEADER_SIZE = 0x20;

			private const int CM_WINBOARD_OR_CM_OFFSET = 0x20; //0x1A for personality, 0 for winboard

			private const int CM_VERSION_OFFSET = 0x24;

			private const int CM_SHOW_ELO_OFFSET = 0x28; //Shows or hides the ELO in the CM GUI (0xB# shows, 0x81 hides)

			private const int CM_PONDER_OFFSET = 0x2C; //1 or 0

			private const int CM_TABLESIZE_OFFSET = 0x30; //Values of 0 to 10-> 0,512K,1MB,2MB,4MB,8MB,16MB,32MB,64MB,128MB,256MB

			private const int CM_ENDGAMEDB_OFFSET = 0x34; //1 or 0

			private const int CM_ELO_OFFSET = 0x38; //User entered ELO

			private const int CM_GM_GROUP_OFFSET = 0x3C; //Part of GM group or not. Value of 1600 is GM.

			private const int CM_ATTACKDEFENSE_OFFSET = 0x40; //Attack defense value (-100 to 100)

			private const int CM_STRENGTH_OFFSET = 0x44; //Strength (0 to 100)

			private const int CM_RANDOMNESS_OFFSET = 0x48; //Positional or material (-100 to 100)

			private const int CM_UNKNOWN_OFFSET = 0x4C; //Unchangeable, always set to 100

			private const int CM_MAXSEARCHDEPTH_OFFSET = 0x50; //Max search depth (1-99)

			private const int CM_SELECTIVESEARCH_OFFSET = 0x54; //Max search depth (1-99)

			private const int CM_CONTEMPT_OFFSET = 0x58; //Contempt for draws (-500 to 500)

			private const int CM_MATERIALPOSITIONAL_OFFSET = 0x5C; //Positional or material (-100 to 100)

			private const int CM_OWNCONTROLOFCENTER_OFFSET = 0x60; //Own control of center (0 to 200)
			private const int CM_OPPCONTROLOFCENTER_OFFSET = 0x64; //Opponet control of center (0 to 200)

			private const int CM_OWNMOBILITY_OFFSET = 0x68; //Own mobility (0 to 200)
			private const int CM_OPPMOBILITY_OFFSET = 0x6C; //Opponet mobility (0 to 200)

			private const int CM_OWNKINGSAFETY_OFFSET = 0x70; //Own king safety (0 to 200)
			private const int CM_OPPKINGSAFETY_OFFSET = 0x74; //Opponet king safety (0 to 200)

			private const int CM_OWNPASSEDPAWNS_OFFSET = 0x78; //Own passed pawns (0 to 200)
			private const int CM_OPPPASSEDPAWNS_OFFSET = 0x7C; //Opponet passed pawns (0 to 200)

			private const int CM_OWNPAWNWEAKNESS_OFFSET = 0x80; //Own pawn weakness (0 to 200)
			private const int CM_OPPPAWNWEAKNESS_OFFSET = 0x84; //Opponet pawn weakness (0 to 200)

			private const int CM_OWNQUEEN_OFFSET = 0x88; //Own queen 0x0 to 0x96 (0 to 15.0)
			private const int CM_OPPQUEEN_OFFSET = 0x8C; //Opponet queen 0x0 to 0x96 (0 to 15.0)

			private const int CM_OWNROOK_OFFSET = 0x90; //Own queen 0x0 to 0x96 (0 to 15.0)
			private const int CM_OPPROOK_OFFSET = 0x94; //Opponet queen 0x0 to 0x96 (0 to 15.0)

			private const int CM_OWNBISHOP_OFFSET = 0x98; //Own queen 0x0 to 0x96 (0 to 15.0)
			private const int CM_OPPBISHOP_OFFSET = 0x9C; //Opponet queen 0x0 to 0x96 (0 to 15.0)

			private const int CM_OWNKNIGHT_OFFSET = 0xA0; //Own queen 0x0 to 0x96 (0 to 15.0)
			private const int CM_OPPKNIGHT_OFFSET = 0xA4; //Opponet queen 0x0 to 0x96 (0 to 15.0)

			private const int CM_OWNPAWN_OFFSET = 0xA8; //Own queen 0x0 to 0x96 (0 to 15.0)
			private const int CM_OPPPAWN_OFFSET = 0xAC; //Opponet queen 0x0 to 0x96 (0 to 15.0)


			private const int CM_UNKNOWN_B0_OFFSET = 0xB0; //always 0
			private const int CM_UNKNOWN_B4_OFFSET = 0xB4; //always 0

			private const int CM_SEX_OFFSET = 0xB8; //1 male, 0 female
			private const int CM_AGE_OFFSET = 0xBC;

			private const int CM_OPENINGBOOK_OFFSET = 0xC0; //Opening book name

			private const int CM_IMAGE_OFFSET = 0x1C4; //Image/portrait for personality

			private const int CM_SHORTPLAYSTYLE_OFFSET = 0x1E2; //Description of playstyle

			private const int CM_BIOGRAPHY_OFFSET = 0x246; //The biography of the personality

			private const int CM_LONGPLAYSTYLE_OFFSET = 0x62E; //Engine specification string (CM Only)

			private const int CM_WINBOARDENGINEPATH_OFFSET = 0xA16; //Path for the winboard engine used by the personality

			public const int CM_MALE = 1;
			public const int CM_FEMALE = 0;
			public const int CM_SHOWELO = 0xB3;
			public const int CM_GROUP = 0x640;    //indicates is a native personality to the game.
			#endregion

			private byte[] header = new Byte[CM_HEADER_SIZE];

			public byte[] Header
			{
				get { return header; }
			}

			public string FilePath { get; set; }
			public string Name { get; set; }
			public string DisplayName
            {
				get
                {
					if (Elo > 0 && ShowElo)
						return $"{Name} ({Elo})";
					return Name;
                }
            }
			public int IsWinboard { get; set; }
			public int Version { get; set; }
			public bool ShowElo { get; set; }
			public int Opk { get; set; }
			public int Elo { get; set; }
			public int IsGM { get; set; }
			/// <summary>
			/// 0 = Female
			/// 1 = Male
			/// </summary>
			public int Sex { get; set; }
			public int Age { get; set; }
			public string Image { get; set; }
			public string ImageFullPath
            {
				get
                {
					if (string.IsNullOrEmpty(Image) || string.IsNullOrEmpty(FilePath))
						return string.Empty;

					string fileName = Path.GetFileNameWithoutExtension(FilePath);
					string fullPath = Path.Combine(Path.GetDirectoryName(FilePath), $"{fileName}.bmp");
					if (File.Exists(fullPath))
						return fullPath;
					return string.Empty;
                }
            }
			public string OpeningBook { get; set; }
			public int AttackDefense { get; set; }
			public int Sop { get; set; } = 100;
			public int MatPos { get; set; }
			public int Rand { get; set; }
			public int MaxDepth { get; set; }
			public int SelSearch { get; set; }
			public int Contempt { get; set; }
			public int TtSize { get; set; }
			public int Ponder { get; set; }
			public int UseEGT { get; set; }
			public int OwnCoC { get; set; }
			public int OppCoC { get; set; }
			public int OwnMob { get; set; }
			public int OppMob { get; set; }
			public int OwnKS { get; set; }
			public int OppKS { get; set; }
			public int OwnPP { get; set; }
			public int OppPP { get; set; }
			public int OwnPW { get; set; }
			public int OppPW { get; set; }
			public int OwnQ { get; set; }
			public int OppQ { get; set; }
			public int OwnR { get; set; }
			public int OppR { get; set; }
			public int OwnB { get; set; }
			public int OppB { get; set; }
			public int OwnN { get; set; }
			public int OppN { get; set; }
			public int OwnP { get; set; }
			public int OppP { get; set; }
			public string ShortPlayingStyle { get; set; }
			public string Biography { get; set; }
			public string LongPlayingStyle { get; set; }
			public string Winboard { get; set; }

			public void Load(string personalityFile)
			{
				this.FilePath = personalityFile;
				this.Name = Path.GetFileNameWithoutExtension(personalityFile);
				byte[] bytes = File.ReadAllBytes(personalityFile);
				Array.Copy(bytes, 0, this.Header, 0, CM_HEADER_SIZE);

				int v = GetIntFromByteArray(bytes, CM_WINBOARD_OR_CM_OFFSET);
				this.IsWinboard = v;

				v = GetIntFromByteArray(bytes, CM_VERSION_OFFSET);
				this.Version = v;

				v = GetIntFromByteArray(bytes, CM_SEX_OFFSET);
				this.Sex = v;

				v = GetIntFromByteArray(bytes, CM_AGE_OFFSET);
				this.Age = v;

				v = GetIntFromByteArray(bytes, CM_SHOW_ELO_OFFSET);
				this.ShowElo = v == 0xB3;

				v = GetIntFromByteArray(bytes, CM_PONDER_OFFSET);
				this.Ponder = v;

				v = GetIntFromByteArray(bytes, CM_TABLESIZE_OFFSET);
				if (v > 0)
					this.TtSize = v;

				v = GetIntFromByteArray(bytes, CM_ENDGAMEDB_OFFSET);
				this.UseEGT = v;

				v = GetIntFromByteArray(bytes, CM_ELO_OFFSET);
				this.Elo = v;

				v = GetIntFromByteArray(bytes, CM_GM_GROUP_OFFSET);
				this.IsGM = v;

				v = GetIntFromByteArray(bytes, CM_ATTACKDEFENSE_OFFSET);
				this.AttackDefense = v;

				v = GetIntFromByteArray(bytes, CM_STRENGTH_OFFSET);
				this.Sop = v;

				v = GetIntFromByteArray(bytes, CM_RANDOMNESS_OFFSET);
				this.Rand = v;

				v = GetIntFromByteArray(bytes, CM_UNKNOWN_OFFSET);
				int unknown = v;

				v = GetIntFromByteArray(bytes, CM_MAXSEARCHDEPTH_OFFSET);
				this.MaxDepth = v;

				v = GetIntFromByteArray(bytes, CM_SELECTIVESEARCH_OFFSET);
				this.SelSearch = v;

				v = GetIntFromByteArray(bytes, CM_CONTEMPT_OFFSET);
				this.Contempt = v;

				v = GetIntFromByteArray(bytes, CM_MATERIALPOSITIONAL_OFFSET);
				this.MatPos = v;

				v = GetIntFromByteArray(bytes, CM_OWNCONTROLOFCENTER_OFFSET);
				this.OwnCoC = v;

				v = GetIntFromByteArray(bytes, CM_OPPCONTROLOFCENTER_OFFSET);
				this.OppCoC = v;

				v = GetIntFromByteArray(bytes, CM_OWNMOBILITY_OFFSET);
				this.OwnMob = v;

				v = GetIntFromByteArray(bytes, CM_OPPMOBILITY_OFFSET);
				this.OppMob = v;

				v = GetIntFromByteArray(bytes, CM_OWNKINGSAFETY_OFFSET);
				this.OwnKS = v;

				v = GetIntFromByteArray(bytes, CM_OPPKINGSAFETY_OFFSET);
				this.OppKS = v;

				v = GetIntFromByteArray(bytes, CM_OWNPASSEDPAWNS_OFFSET);
				this.OwnPP = v;

				v = GetIntFromByteArray(bytes, CM_OPPPASSEDPAWNS_OFFSET);
				this.OppPP = v;

				v = GetIntFromByteArray(bytes, CM_OWNPAWNWEAKNESS_OFFSET);
				this.OwnPW = v;

				v = GetIntFromByteArray(bytes, CM_OPPPAWNWEAKNESS_OFFSET);
				this.OppPW = v;

				v = GetIntFromByteArray(bytes, CM_OWNQUEEN_OFFSET);
				this.OwnQ = v;

				v = GetIntFromByteArray(bytes, CM_OPPQUEEN_OFFSET);
				this.OppQ = v;

				v = GetIntFromByteArray(bytes, CM_OWNROOK_OFFSET);
				this.OwnR = v;

				v = GetIntFromByteArray(bytes, CM_OPPROOK_OFFSET);
				this.OppR = v;

				v = GetIntFromByteArray(bytes, CM_OWNBISHOP_OFFSET);
				this.OwnB = v;

				v = GetIntFromByteArray(bytes, CM_OPPBISHOP_OFFSET);
				this.OppB = v;

				v = GetIntFromByteArray(bytes, CM_OWNKNIGHT_OFFSET);
				this.OwnN = v;

				v = GetIntFromByteArray(bytes, CM_OPPKNIGHT_OFFSET);
				this.OppN = v;

				v = GetIntFromByteArray(bytes, CM_OWNPAWN_OFFSET);
				this.OwnP = v;

				v = GetIntFromByteArray(bytes, CM_OPPPAWN_OFFSET);
				this.OppP = v;

				string openingBook = GetStringFromByteArray(bytes, CM_OPENINGBOOK_OFFSET);
				this.OpeningBook = openingBook;

				string image = GetStringFromByteArray(bytes, CM_IMAGE_OFFSET);
				this.Image = image;

				string playingStyle = GetStringFromByteArray(bytes, CM_SHORTPLAYSTYLE_OFFSET);
				this.ShortPlayingStyle = playingStyle;

				string biography = GetStringFromByteArray(bytes, CM_BIOGRAPHY_OFFSET);
				this.Biography = biography;

				string longPlayStyle = GetStringFromByteArray(bytes, CM_LONGPLAYSTYLE_OFFSET);
				longPlayStyle = longPlayStyle.Replace("%d", this.Elo.ToString());
				this.LongPlayingStyle = longPlayStyle;

				string wbEnginePath = GetStringFromByteArray(bytes, CM_WINBOARDENGINEPATH_OFFSET);
				this.Winboard = wbEnginePath;
			} // Load

			public static List<Personality> GetFromFolder(string path)
            {
				List<Personality> res = new List<Personality>();
				if (string.IsNullOrEmpty(path))
					return res;

				if (Directory.Exists(path)) {
					foreach (var file in Directory.GetFiles(path, "*.CMP")) {
						var p = new Personality();
						p.Load(file);

						res.Add(p);
					}
				}
				return res;
			} // GetFromFolder

			private int GetIntFromByteArray(byte[] bytes, int offset)
			{
				return bytes[offset + 3] << 24 | bytes[offset + 2] << 16 | bytes[offset + 1] << 8 | bytes[offset];
			} // GetIntFromByteArray

			private string GetStringFromByteArray(byte[] bytes, int offset)
			{
				int inc = offset;
				byte b = bytes[inc++];

				StringBuilder sb = new StringBuilder();
				while (b != 0) {
					sb.Append((char)b);
					b = bytes[inc++];
				}
				return sb.ToString();
			}
		} // Personality
		#endregion

		private int? m_Elo = null;
		public const string PersonalitiesFolderOptionName = "Personalities folder";
		public const string OpeningBooksFolderOptionName = "Opening books folder";

		public TheKing(string name, string command)
			: base(name, command)
		{

		}

        public override async Task<bool> ApplyOptions(bool onlyModified)
		{
			await SetOption("cm_parm", "default");
			await base.ApplyOptions(onlyModified);

			return true;
		} // ApplyOptions

		/// <summary>
		/// Set the engine options based on the given personality
		/// </summary>
		/// <param name="pers"></param>
		/// <returns></returns>
		public async Task<bool> ApplyPersonality(Personality pers)
		{
			await WriteCommand($"cm_parm opp={pers.OppP} opn={pers.OppN} opb={pers.OppB} opr={pers.OppR} opq={pers.OppQ}");
			await WriteCommand($"cm_parm myp={pers.OwnP} myn={pers.OwnN} myb={pers.OwnB} myr={pers.OwnR} myq={pers.OwnQ}");
			await WriteCommand($"cm_parm mycc={pers.OwnCoC} mymob={pers.OwnMob} myks={pers.OwnKS} mypp={pers.OwnPP} mypw={pers.OwnPW}");
			await WriteCommand($"cm_parm opcc={pers.OppCoC} opmob={pers.OppMob} opks={pers.OppKS} oppp={pers.OppPP} oppw={pers.OppPW}");
			await WriteCommand($"cm_parm cfd={pers.Contempt} sop={pers.Sop} avd={pers.AttackDefense} rnd={pers.Rand} sel={pers.SelSearch} md={pers.MaxDepth}");
			await WriteCommand($"cm_parm tts={Math.Pow(2, 18 + pers.TtSize)}");
			await WaitPong();

			m_Elo = pers.Elo;			
			return true;
		} // ApplyPersonality

		public override int GetDefaultAnalyzeDepth()
		{
			return 7003;
		} // GetDefaultAnalyzeDepth

		protected override void ParseOptions()
		{
			if (Options.Count == 0) {
				Options.Add(new Option() { Name = PonderOptionName, Type = "check", Default = "true", Value = "true" });
				Options.Add(new Option() { Name = PersonalitiesFolderOptionName, Type = "path", Internal = true });
				Options.Add(new Option() { Name = OpeningBooksFolderOptionName, Type = "path", Internal = true });
			}
		}

        protected override async Task<bool> WaitPong()
        {
			// TheKing does not support ping
			await WriteCommand($"ping 1");
			await ReadOutput("Error (unknown command): ping");

			return true;
		} // WaitPong


        public override int? GetElo()
        {
            return m_Elo;
        } // GetElo		
    }
}