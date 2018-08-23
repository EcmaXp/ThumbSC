using System.Diagnostics.CodeAnalysis;

namespace ThumbSC
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "IdentifierTypo")]
    public class Registers
    {
        private readonly int[] _regs;

        public Registers()
        {
            _regs = new int[RegisterIndex.CPSR + 1];
            _regs[RegisterIndex.CPSR] = (
                BinaryParseHelper.FT | // set Thumb
                BinaryParseHelper.FZ // set Zero
            );
        }

        // ReSharper disable once UnusedMember.Global
        public int[] Load()
        {
            var regs = new int[RegisterIndex.CPSR + 1];
            Load(regs);
            return regs;
        }

        public void Load(int[] regs)
        {
            _regs.CopyTo(regs, 0);
        }

        public void Store(int[] regs)
        {
            regs.CopyTo(_regs, 0);
        }

        // ReSharper disable UnusedMember.Global
        public int R0
        {
            get => _regs[RegisterIndex.R0];
            set => _regs[RegisterIndex.R0] = value;
        }

        public int R1
        {
            get => _regs[RegisterIndex.R1];
            set => _regs[RegisterIndex.R1] = value;
        }

        public int R2
        {
            get => _regs[RegisterIndex.R2];
            set => _regs[RegisterIndex.R2] = value;
        }

        public int R3
        {
            get => _regs[RegisterIndex.R3];
            set => _regs[RegisterIndex.R3] = value;
        }

        public int R4
        {
            get => _regs[RegisterIndex.R4];
            set => _regs[RegisterIndex.R4] = value;
        }

        public int R5
        {
            get => _regs[RegisterIndex.R5];
            set => _regs[RegisterIndex.R5] = value;
        }

        public int R6
        {
            get => _regs[RegisterIndex.R6];
            set => _regs[RegisterIndex.R6] = value;
        }

        public int R7
        {
            get => _regs[RegisterIndex.R7];
            set => _regs[RegisterIndex.R7] = value;
        }

        public int R8
        {
            get => _regs[RegisterIndex.R8];
            set => _regs[RegisterIndex.R8] = value;
        }

        public int R9
        {
            get => _regs[RegisterIndex.R9];
            set => _regs[RegisterIndex.R9] = value;
        }

        public int R10
        {
            get => _regs[RegisterIndex.R10];
            set => _regs[RegisterIndex.R10] = value;
        }

        public int R11
        {
            get => _regs[RegisterIndex.R11];
            set => _regs[RegisterIndex.R11] = value;
        }

        public int R12
        {
            get => _regs[RegisterIndex.R12];
            set => _regs[RegisterIndex.R12] = value;
        }

        public int SP
        {
            get => _regs[RegisterIndex.SP];
            set => _regs[RegisterIndex.SP] = value;
        }

        public int LR
        {
            get => _regs[RegisterIndex.LR];
            set => _regs[RegisterIndex.LR] = value;
        }

        public int PC
        {
            get => _regs[RegisterIndex.PC];
            set => _regs[RegisterIndex.PC] = value;
        }

        public int CPSR
        {
            get => _regs[RegisterIndex.CPSR];
            set => _regs[RegisterIndex.CPSR] = value;
        }
        // ReSharper restore UnusedMember.Global
    }
}