using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace ChessLib.EBoards
{
    public class DGT : EBoard
    {
        #region classes
        public class DGTSettings : EBoard.EBoardSettings
        {
            public DGTSettings()
            {
                Name = "DGT";
                ReadTimeout = 500;
                WriteTimeout = 500;
            }

            public string PortName { get; set; }
            public int ReadTimeout { get;set; }
            public int WriteTimeout { get; set; }
        } // DGTSettings

        public class DgtClock
        {
            public string Name { get; set; }
            public Version Version { get; set; }
        } // DgtClock

        class BoardMessage {
            public BoardMessage(BoardToPcMessages message, byte[] data) {
                Message = message;
                Data = data;
            }
            public BoardToPcMessages Message { get; private set; }
            public byte[] Data { get; private set; }

            public string StringData {
                get {
                    if (Data != null)
                        return Encoding.ASCII.GetString(Data);
                    return null;
                }
            }
        } // BoardMessage

        public class ClockAck {
            public ClockAck(int ack0, int ack1, int ack2, int ack3) {
                Ack0 = ack0;
                Ack1 = ack1;
                Ack2 = ack2;
                Ack3 = ack3;
            }

            public int Ack0 { get; private set; }
            public int Ack1 { get; private set; }
            public int Ack2 { get; private set; }
            public int Ack3 { get; private set; }
        } // ClockAck

        public class BatteryStatus
        {
            public int CapacityLeft { get; set; }
            public TimeSpan? TimeLeft { get; set; }
            public TimeSpan OnTime { get; set; }
            public TimeSpan Standby { get; set; }
            public bool Charging { get; set; }
        } // BatteryStatus
        #endregion

        #region defines
        enum Modes
        {
            Idle,
            UpdateBoard,
            UpdateSbi,
            UpdateSbiNice,
            UpdateI2C,
            UpdateI2CNice,
            Bus
        }

        enum PcToBoardCommands
        {
            DGT_REQ_RESET = 0x40,
            DGT_REQ_SBI_CLOCK = 0x41,
            DGT_REQ_BOARD = 0x42,
            DGT_REQ_UPDATE_SBI = 0x43,
            DGT_REQ_UPDATE_BOARD = 0x44,
            DGT_REQ_SERIALNR = 0x45,
            DGT_REQ_BUASDDRESS = 0x46,
            DGT_REQ_TRADEMARK = 0x47,
            DGT_REQ_HARDWARE_VERSION = 0x48,
            DGT_REQ_LOG_MOVES = 0x49,
            DGT_REQ_TO_BUSMODE = 0x4a,
            DGT_REQ_UPDATE_SBI_NICE = 0x4b,
            DGT_REQ_BATTERY_STATUS = 0x4c,
            DGT_REQ_VERSION = 0x4d,
            DGT_REQ_LONG_SERIALNR = 0x55,
            DGT_REQ_I2C_CLOCK = 0x56,
            DGT_REQ_UPDATE_I2C = 0x57,
            DGT_REQ_UPDATE_I2C_NICE = 0x58,

            DGT_SBI_CLOCK_MESSAGE = 0x2b,
            DGT_I2C_CLOC_MESSAGE = 0x2c,
            DGT_SET_LEDS = 0x60,

            DGT_BUS_REQ_SBI_CLOCK = 0x81,
            DGT_BUS_REQ_BOARD = 0x82,
            DGT_BUS_REQ_CHANGES = 0x83,
            DGT_BUS_REPEAT_CHANGES = 0x8,
            DGT_BUS_SET_START_GAME = 0x85,
            DGT_BUS_REQ_FROM_START = 0x86,
            DGT_BUS_PING = 0x87,
            DGT_BUS_END_BUSMODE = 0x88,
            DGT_BUS_RESET = 0x89,
            DGT_BUS_IGNORE_NEXT_BUS_PING = 0x8a,
            DGT_BUS_REQ_VERSION = 0x8b,
            DGT_BUS_REQ_ALL_D = 0x8d,
            DGT_BUS_REQ_SERIALNR = 0x91,
            DGT_BUS_RPING = 0x92,
            DGT_BUS_REQ_I2X_CLOCK = 0x93,
            DGT_BUS_REQ_I2C_BUTTON = 0x94,
            DGT_BUS_REQ_I2C_ACK = 0x95,
            DGT_BUS_REQ_STATS = 0x96,
            DGT_BUS_REQ_TRADEMARK = 0x97,
            DGT_BUS_REQ_BATTERY = 0x98,
            DGT_BUS_REPEAT_I2C_BUTTON = 0x99,
            DGT_BUS_REPEAT_I2C_ACK = 0x9a,
            DGT_BUS_REQ_HARDWARE_VERSION = 0x9b,

            DGT_BUS_CONFIG = 0xa0,
            DGT_BUS_SBI_CLOCK_MESSAGE = 0xa1,
            DGT_BUS_I2C_CLOCK_MESSAGE = 0xa2,

            DGT_CMD_CLOCK_DISPLAY = 0x01,
            DGT_CMD_CLOCK_ICONS = 0x02,
            DGT_CMD_CLOCK_END = 0x03,
            DGT_CMD_CLOCK_BUTTON = 0x08,
            DGT_CMD_CLOCK_VERSION = 0x09,
            DGT_CMD_CLOCK_SETNRUN = 0x0a,
            DGT_CMD_CLOCK_BEEP = 0x0b,
            DGT_CMD_CLOCK_ASCII = 0x0c,
            DGT_CMD_REV2_ASCII = 0x0d,
            DGT_CMD_CLOCK_START_MESSAGE = 0x03,
            DGT_CMD_CLOCK_END_MESSAGE = 0x00,

        } // PcToBoardCommands

        enum BoardToPcMessages
        {
            DGT_MSG_BOARD_DUMP = 0x86,
            DGT_MSG_HARDWARE_VERSION = 0x96,
            DGT_MSG_SBI_CLOCK = 0x8d,
            DGT_MSG_FIELD_UPDATE = 0x8e,
            DGT_MSG_LOG_MOVES = 0x8f,
            DGT_MSG_BUSADDRESS = 0x90,
            DGT_MSG_SERIALNR = 0x91,
            DGT_MSG_TRADEMARK = 0x92,
            DGT_MSG_VERSION = 0x93,
            DGT_MSG_BATTERY_STATUS = 0xa0,
            DGT_MSG_LONG_SERIALNR = 0xa2,
            DGT_MSG_I2C_CLOCK = 0xa3,

            DGT_MSG_BUS_BOARD_DUMP = 0x83,
            DGT_MSG_BUS_SBI_CLOCK = 0x84,
            DGT_MSG_BUS_UPDATE_ODD = 0x85,
            DGT_MSG_BUS_FROM_START = 0x86,
            DGT_MSG_BUS_PING = 0x87,
            DGT_MSG_BUS_START_GAME_WRITTEN = 0x88,
            DGT_MSG_BUS_VERSION = 0x89,
            DGT_MSG_BUS_UPDATE_EVEN = 0x8b,
            DGT_MSG_BUS_HARDWARE_VERSION = 0x8c,
            DGT_MSG_BUS_SERIALNR = 0xb0,
            DGT_MSG_BUS_I2C = 0xb1,
            DGT_MSG_BUS_STATS = 0xb2,
            DGT_MSG_BUS_CONFIG = 0xb3,
            DGT_MSG_BUS_TRADEMARK = 0xb4,
            DGT_MSG_BUS_BATTERY_STATUS = 0xb5

        } // BoardToPcMessages

        enum LogFileMessages
        {
            LOG_NOP2 = 0x00,
            LOG_SUBSEC_DELAY = 0x10,
            LOG_SEC_DELAY = 0x10,
            LOG_MIN_DELAY = 0x20,
            LOG_HOUR_DELAY = 0x30,
            LOG_FIELD_UPDATE = 0x40,
            LOG_FIELD_UPDATE_AS = 0x50,
            LOG_CLOCK_UPDATE_R = 0x60,
            LOG_POWERUP = 0x6a,
            LOG_EOF = 0x6b,
            LOG_FOURROWS = 0x6c,
            LOG_EMPTY_BOARD = 0x6d,
            LOG_DOWNLOADED = 0x6e,
            LOG_START_POS = 0x6f,
            LOG_CLOCK_UPDATE_L = 0x70,
            LOG_START_POS_ROT = 0x7a,
            LOG_START_TAG = 0x7b,
            LOG_DEBUG = 0x7c,
            LOG_NEW_CLOCK_STATE = 0x7d,
            LOG_NEW_BUS_ADDRESS = 0x7e,
            LOG_EMPTY = 0xff
        }

        enum Pieces
        {
            Empty = 0x00,
            WhitePawn = 0x01,
            WhiteRook = 0x02,
            WhiteKnight = 0x03,
            WhiteBishop = 0x04,
            WhiteKing = 0x05,
            WhiteQueen = 0x06,

            BlackPawn = 0x07,
            BlackRook = 0x08,
            BlackKnight = 0x09,
            BlackBishop = 0x0a,
            BlackKing = 0x0b,
            BlackQueen = 0x0c,

            Draw = 0x0d,
            WhiteWins = 0x0e,
            BlackWins = 0x0f
        }
        #endregion

        #region events
        public class BoardUpdateArgs : EventArgs
        {
            public BoardUpdateArgs(string position)
            {
                Position = position;
            }

            public string Position { get; set; }
        }
        public delegate void BoardUpdateHandler(object sender, BoardUpdateArgs e);
        public event BoardUpdateHandler BoardUpdate;

        public class ClockUpdateArgs : EventArgs
        {
            public ClockUpdateArgs()
            {
            }

            public bool IsAck { get { return Ack != null; } }
            public ClockAck Ack { get; set; }

            public TimeSpan? RightPlayerTime { get; set; }
            public bool RightPlayerTimePerMoveIndicatorOn { get; set; }
            public bool RightPlayerFlag1 { get; set; }
            public bool RightPlayerFlag2 { get; set; }

            public TimeSpan? LeftPlayerTime { get; set; }
            public bool LeftPlayerTimePerMoveIndicatorOn { get; set; }
            public bool LeftPlayerFlag1 { get; set; }
            public bool LeftPlayerFlag2 { get; set; }

            public bool Running { get; set; }
            public bool RightPlayerLeverHigh { get; set; }
            public bool LeftPlayerLeverHigh { get { return !RightPlayerLeverHigh; } }
            public bool BatteryLow { get; set; }
            public bool LeftPlayerTurn { get; set; }
            public bool RightPlayerTurn { get; set; }
            public bool ClockConnected { get; set; }
        }
        public delegate void ClockUpdateHandler(object sender, ClockUpdateArgs e);
        public event ClockUpdateHandler ClockUpdate;
        #endregion

        private SerialPort m_Port = null;

        private Semaphore m_MessagesSema = new Semaphore(1, 1);
        private List<BoardMessage> m_Messages = new List<BoardMessage>();

        public DGT(DGTSettings settings) :
            base(settings)
        {

        }

        private DGTSettings LocalSettings { get { return (DGTSettings)Settings; } }

        public Version Version { get; private set; }
        public Version HardwareVersion { get; private set; }
        public string SerialNumber { get; private set; }

        public DgtClock Clock { get; private set; }

        public override async Task<bool> Init()
        {
            m_Port = new SerialPort();
            m_Port.PortName = LocalSettings.PortName;
            m_Port.BaudRate = 9600;
            m_Port.DataBits = 8;
            m_Port.Parity = Parity.None;
            m_Port.StopBits = StopBits.One;
            m_Port.Handshake = Handshake.None;
            m_Port.ReadTimeout = LocalSettings.ReadTimeout;
            m_Port.WriteTimeout = LocalSettings.WriteTimeout;

            m_Port.DataReceived += OnDataReceived;
            m_Port.ErrorReceived += OnErrorReceived;
            try {
                m_Port.Open();

                Version = await GetVersion();
                HardwareVersion = await GetHardwareVersion();
                SerialNumber = await GetSerialNumber();

                // Check clock
                var cv = await GetClockVersion();
                if (cv != null) {
                    Clock = new DgtClock();
                    Clock.Version = cv;
                }

            } catch {
                return false;
            }
            return true;
        }

        public override void Dispose()
        {
            if (m_Port != null) {
                if (m_Port.IsOpen)
                    m_Port.Close();
                m_Port.Dispose();
            }

            if (m_MessagesSema != null) {
                m_MessagesSema.Dispose();
                m_MessagesSema = null;
            }
        }

        public async Task<Version> GetVersion()
        {
            if (await WriteCommand(PcToBoardCommands.DGT_REQ_VERSION)) {
                var res = await WaitMessage(BoardToPcMessages.DGT_MSG_VERSION);
                if (res != null)
                    return new Version(res.Data[0], res.Data[1]);
            }
            return null;
        } // GetVersion

        public async Task<Version> GetHardwareVersion()
        {
            if (await WriteCommand(PcToBoardCommands.DGT_REQ_HARDWARE_VERSION)) {
                var res = await WaitMessage(BoardToPcMessages.DGT_MSG_HARDWARE_VERSION);
                if (res != null)
                    return new Version(res.Data[0], res.Data[1]);
            }
            return null;
        } // GetHardwareVersion

        public async Task<string> GetSerialNumber()
        {
            if (await WriteCommand(PcToBoardCommands.DGT_REQ_SERIALNR)) {
                var res = await WaitMessage(BoardToPcMessages.DGT_MSG_SERIALNR);
                if (res != null)
                    return res.StringData;
            }
            return string.Empty;
        } // GetSerialNumber

        public async Task<string> GetLongSerialNumber()
        {
            if (await WriteCommand(PcToBoardCommands.DGT_REQ_LONG_SERIALNR)) {
                var res = await WaitMessage(BoardToPcMessages.DGT_MSG_LONG_SERIALNR);
                if (res != null)
                    return res.StringData;
            }
            return string.Empty;
        } // GetLongSerialNumber

        public async Task<string> GetTrademark()
        {
            if (await WriteCommand(PcToBoardCommands.DGT_REQ_TRADEMARK)) {
                var res = await WaitMessage(BoardToPcMessages.DGT_MSG_TRADEMARK);
                if (res != null)
                    return res.StringData;
            }
            return string.Empty;
        } // GetTrademark

        public async Task<string> GetBusAddress()
        {
            if (await WriteCommand(PcToBoardCommands.DGT_REQ_BUASDDRESS)) {
                var res = await WaitMessage(BoardToPcMessages.DGT_MSG_BUSADDRESS);
                if (res != null)
                    return res.StringData;
            }
            return string.Empty;
        } // GetBusAddress

        public async Task<BatteryStatus> GetBatteryStatus()
        {
            if (await WriteCommand(PcToBoardCommands.DGT_REQ_BATTERY_STATUS)) {
                var res = await WaitMessage(BoardToPcMessages.DGT_MSG_BATTERY_STATUS);
                if (res != null) {
                    if (res.Data.Length != 9)
                        throw new Exception("Invalid battery data");

                    var bs = new BatteryStatus();
                    bs.CapacityLeft = res.Data[0];
                    bs.Charging = res.Data[8] == 0;

                    if (res.Data[1] != 0x7f && res.Data[2] != 0x7f)
                        bs.TimeLeft = new TimeSpan(res.Data[1], res.Data[2], 0);

                    bs.OnTime = new TimeSpan(res.Data[1], res.Data[2], 0);
                    bs.OnTime = new TimeSpan(res.Data[5], res.Data[6], res.Data[7], 0);

                    return bs;
                }
            }

            return null;
        } // GetBatteryStatus

        public async Task<bool> Reset()
        {
            return await WriteCommand(PcToBoardCommands.DGT_REQ_RESET);
        } // Reset

        public override async Task<string> GetBoard()
        {
            if (await WriteCommand(PcToBoardCommands.DGT_REQ_BOARD)) {
                var res = await WaitMessage(BoardToPcMessages.DGT_MSG_BOARD_DUMP);
                if (res != null)
                    return GetFen(res.Data);
            }
            return string.Empty;
        } // GetBoard

        /// <summary>
        /// The board will automatically send board update whn a piece is moved
        /// </summary>
        /// <returns></returns>
        public async Task<bool> RequestBoardUpdate()
        {
            return await WriteCommand(PcToBoardCommands.DGT_REQ_UPDATE_BOARD);
        } // RequestBoardUpdate

        #region clock
        public async Task<Version> GetClockVersion()
        {
            if (await WriteClockCommand(PcToBoardCommands.DGT_CMD_CLOCK_VERSION)) {
                var res = await WaitMessage(BoardToPcMessages.DGT_MSG_SBI_CLOCK);
                if (res != null) {
                    var ack = ReadClockAckMessage(res.Data);
                    return new Version(ack.Ack2 >> 4, ack.Ack2 & 0x0f);
                }
            }
            return null;
        } // GetClockVersion

        public async Task<bool> ClockBeep(int time)
        {
            if (await WriteClockCommand(PcToBoardCommands.DGT_CMD_CLOCK_BEEP, new byte[1] { (byte)time })) {
                var res = await WaitMessage(BoardToPcMessages.DGT_MSG_SBI_CLOCK);
                if (res != null) {
                    var ack = ReadClockAckMessage(res.Data);
                    return ack.Ack1 == 0x0b;
                }
            }
            return false;
        } // ClockBeep
        #endregion

        #region private operations
        private async Task<bool> WriteCommand(PcToBoardCommands message, byte[] data = null)
        {
            if (data == null) {
                byte[] buffer = new byte[1];
                buffer[0] = (byte)message;

                m_Port.Write(buffer, 0, buffer.Length);
            } else {
                if (data.Length > 126)
                    throw new Exception("Data too long");
                int size = data.Length + 3;
                byte[] buffer = new byte[size];

                buffer[0] = (byte)message;
                buffer[1] = (byte)((data.Length + 1) << 7);
                buffer[buffer.Length - 1] = 0;

                Buffer.BlockCopy(data, 0, buffer, 2, data.Length);

                m_Port.Write(buffer, 0, buffer.Length);
            }
            await Task.Delay(100);

            return true;
        } // WriteCommand

        private async Task<bool> WriteCommandString(PcToBoardCommands message, string data = null)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));

            Dictionary<char, byte> chars = new Dictionary<char, byte>()
            {
                {'0', 0x3f}, {'1', 0x06}, {'2', 0x5b}, {'3', 0x4f}, {'4', 0x66}, {'5', 0x6d}, {'6', 0x7d}, {'7', 0x07},
                {'8', 0x7f}, {'9', 0x6f}, {'a', 0x5f}, {'b', 0x7c}, {'c', 0x58}, {'d', 0x5e}, {'e', 0x7b}, {'f', 0x71},
                {'g', 0x3d}, {'h', 0x74}, {'i', 0x10}, {'j', 0x1e}, {'k', 0x75}, {'l', 0x38}, {'m', 0x55}, {'n', 0x54},
                {'o', 0x5c}, {'p', 0x73}, {'q', 0x67}, {'r', 0x50}, {'s', 0x6d}, {'t', 0x78}, {'u', 0x3e}, {'v', 0x2a},
                {'w', 0x7e}, {'x', 0x64}, {'y', 0x6e}, {'z', 0x5b}, {' ', 0x00}, {'-', 0x40}, {'/', 0x52}, {'|', 0x36},
                {'\\', 0x64}, {'?', 0x53}, {'@', 0x65}, {'=', 0x48}, {'_', 0x08}
            };

            byte[] dataBytes = new byte[data.Length];

            data = data.ToLower();
            for (int i = 0; i < data.Length; i++) {
                byte b;
                if (chars.TryGetValue(data[i], out b)) {
                    dataBytes[i] = b;
                }
            }
            return await WriteCommand(message, dataBytes);
        } // WriteCommandString


        private async Task<bool> WriteClockCommand(PcToBoardCommands message, byte[] data = null)
        {
            int size = 5;
            if (data != null)
                size += data.Length;
            byte[] buffer = new byte[size];

            buffer[0] = (int)PcToBoardCommands.DGT_SBI_CLOCK_MESSAGE;
            buffer[1] = (byte)size;
            buffer[2] = (int)PcToBoardCommands.DGT_CMD_CLOCK_START_MESSAGE;
            buffer[3] = (byte)message;

            if (data != null)
                Buffer.BlockCopy(data, 0, buffer, 4, data.Length);

            buffer[buffer.Length - 1] = (int)PcToBoardCommands.DGT_CMD_CLOCK_END_MESSAGE;

            m_Port.Write(buffer, 0, buffer.Length);
            await Task.Delay(100);

            return true;
        } // WriteClockCommand

        private bool IsClockAckMessage(byte[] data)
        {
            if (data.Length != 10)
                return false;

            return ((data[0] & 0x0f) == 0x0a || (data[3] & 0x0f) == 0x0a);
        } // IsClockAckMessage

        private ClockAck ReadClockAckMessage(byte[] data)
        {
            if (!IsClockAckMessage(data))
                throw new Exception("Invalid clock ACK data");

            var ack0 = ((data[1]) & 0x7f) | ((data[3] << 3) & 0x80);
            var ack1 = ((data[2]) & 0x7f) | ((data[3] << 2) & 0x80);
            var ack2 = ((data[4]) & 0x7f) | ((data[0] << 3) & 0x80);
            var ack3 = ((data[5]) & 0x7f) | ((data[0] << 2) & 0x80);

            return new ClockAck(ack0, ack1, ack2, ack3);
        } // ReadClockAckMessage

        private BoardMessage ReadMessage()
        {
            byte[] header = new byte[3];
            m_Port.Read(header, 0, 3);

            int message = header[0];
            int size = (header[1] << 7) + header[2] - 3;

            if (size > 3) {
                byte[] buffer = new byte[size];
                m_Port.Read(buffer, 0, buffer.Length);
                return new BoardMessage((BoardToPcMessages)message, buffer);
            }

            return new BoardMessage((BoardToPcMessages)message, null);
        } // ReadMessage

        private async Task<BoardMessage> WaitMessage(BoardToPcMessages message, int timeoutMs = 1000)
        {
            DateTime started = DateTime.UtcNow;
            while(true) {
                m_MessagesSema.WaitOne();
                var msg = m_Messages.Where(m => m.Message == message).FirstOrDefault();
                if (msg != null)
                    m_Messages.Remove(msg);
                m_MessagesSema.Release();

                if (msg != null)
                    return msg;
                else
                    await Task.Delay(20);

                if (timeoutMs > 0 && (DateTime.UtcNow - started).TotalMilliseconds >= timeoutMs)
                    return null;
            }
        } // WaitMessage

        private string GetFen(byte[] bytes)
        {
                if (bytes.Length != 64)
                    throw new Exception("Invalid board data");

                StringBuilder sb = new StringBuilder();
                int n = 0;
                for (int i = 0; i < 64; i++) {
                    if (i % 8 == 0)
                        sb.Append("/");

                    byte p = bytes[i];
                    string piece = string.Empty;
                    switch (p) {
                        case (byte)Pieces.WhitePawn:
                            piece = "P";
                            break;
                        case (byte)Pieces.WhiteKnight:
                            piece = "N";
                            break;
                        case (byte)Pieces.WhiteBishop:
                            piece = "B";
                            break;
                        case (byte)Pieces.WhiteRook:
                            piece = "R";
                            break;
                        case (byte)Pieces.WhiteQueen:
                            piece = "Q";
                            break;
                        case (byte)Pieces.WhiteKing:
                            piece = "K";
                            break;

                        case (byte)Pieces.BlackPawn:
                            piece = "p";
                            break;
                        case (byte)Pieces.BlackKnight:
                            piece = "n";
                            break;
                        case (byte)Pieces.BlackBishop:
                            piece = "b";
                            break;
                        case (byte)Pieces.BlackRook:
                            piece = "e";
                            break;
                        case (byte)Pieces.BlackQueen:
                            piece = "q";
                            break;
                        case (byte)Pieces.BlackKing:
                            piece = "k";
                            break;
                    }

                    if (!string.IsNullOrEmpty(piece)) {
                        if (n != 0)
                            sb.Append(n);
                        sb.Append(piece);
                        n = 0;
                    } else {
                        n++;
                    }
                }

                if (n > 0)
                    sb.Append(n);

            return sb.ToString();
        } // GetFen

        private void AddMessageToQueue(BoardMessage msg)
        {
            m_MessagesSema.WaitOne();
            m_Messages.Add(msg);
            m_MessagesSema.Release();
        } // AddMessageToQueue

        private int GetIntValue(System.Collections.BitArray bits, int start, int length)
        {
            int value = 0;
            for (int i = start; i < start + length; i++) {
                if (bits[i])
                    value += Convert.ToInt16(Math.Pow(2, i - start));
            }

            return value;
        } // GetIntValue

        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            var msg = ReadMessage();
            if (msg.Message == BoardToPcMessages.DGT_MSG_FIELD_UPDATE) {
                BoardUpdate?.Invoke(this, new BoardUpdateArgs(GetFen(msg.Data)));
            } else if (msg.Message == BoardToPcMessages.DGT_MSG_SBI_CLOCK) {
                if (IsClockAckMessage(msg.Data)) {
                    // Check if this is an automatic message:
                    var ack = ReadClockAckMessage(msg.Data);
                    var bits = new System.Collections.BitArray((byte)ack.Ack1);
                    if (bits[7] == true)
                        ClockUpdate?.Invoke(this, new ClockUpdateArgs() { Ack = ack });
                    else
                        AddMessageToQueue(msg);
                } else {
                    var clockArgs = new ClockUpdateArgs();

                    // Right player
                    var bits = new System.Collections.BitArray((byte)msg.Data[3]);
                    clockArgs.RightPlayerTime = new TimeSpan(GetIntValue(bits, 0, 3), msg.Data[4], msg.Data[5]);
                    clockArgs.RightPlayerFlag1 = bits[4];
                    clockArgs.RightPlayerTimePerMoveIndicatorOn = bits[5];
                    clockArgs.RightPlayerFlag2 = bits[6];

                    // Left player
                    bits = new System.Collections.BitArray((byte)msg.Data[6]);
                    clockArgs.LeftPlayerTime = new TimeSpan(GetIntValue(bits, 0, 3), msg.Data[7], msg.Data[8]);
                    clockArgs.LeftPlayerFlag1 = bits[4];
                    clockArgs.LeftPlayerTimePerMoveIndicatorOn = bits[5];
                    clockArgs.LeftPlayerFlag2 = bits[6];

                    // Clock status
                    bits = new System.Collections.BitArray((byte)msg.Data[9]);
                    clockArgs.Running = bits[0];
                    clockArgs.RightPlayerLeverHigh = bits[1];
                    clockArgs.BatteryLow = bits[2];
                    clockArgs.LeftPlayerTurn = bits[3];
                    clockArgs.RightPlayerTurn = bits[4];
                    clockArgs.ClockConnected = bits[5];

                    ClockUpdate?.Invoke(this, clockArgs);
                }
            } else {
                AddMessageToQueue(msg);
            }
        } // OnDataReceived

        private void OnErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {

        } // OnErrorReceived
        #endregion
    }
}