using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using static ThumbSC.BinaryParseHelper;

namespace ThumbSC
{
    public class ThumbSC
    {
        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global
        public Registers Regs { get; set; } = new Registers();

        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global
        public Memory Memory { get; set; } = new Memory();

        // ReSharper disable once IdentifierTypo
        public virtual void Interrupt(byte soffset)
        {
            throw new NotImplementedException();
        }

        [SuppressMessage("ReSharper", "SwitchStatementMissingSomeCases")]
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [SuppressMessage("ReSharper", "IdentifierTypo")]
        [SuppressMessage("ReSharper", "CommentTypo")]
        // ReSharper disable once UnusedMember.Global
        public void Run(int count)
        {
            var memory = Memory;
            var REGS = new int[RegisterIndex.CPSR + 1];

            Regs.Load(REGS);
            var q = (REGS[RegisterIndex.CPSR] & FQ) != 0;
            var v = (REGS[RegisterIndex.CPSR] & FV) != 0;
            var c = (REGS[RegisterIndex.CPSR] & FC) != 0;
            var z = (REGS[RegisterIndex.CPSR] & FZ) != 0;
            var n = (REGS[RegisterIndex.CPSR] & FN) != 0;

            Debug.Assert((REGS[RegisterIndex.PC] & I0) == 0);
            REGS[RegisterIndex.PC] &= ~I0;

            try
            {
                while (count-- > 0)
                {
                    var incr_pc = true;
                    int Rs, Rd, Rb;
                    int left, right, value, addr;
                    ulong lvalue;
                    uint uleft, uvalue;
                    bool L, B, S, H;
                    int list;

                    var code = memory.ReadShort(REGS[RegisterIndex.PC]);

                    switch ((code >> 12) & L4)
                    {
                        case 0b0000:
                        case 0b0001: // :000x
                            Rs = (code >> 3) & L3;
                            Rd = code & L3;
                            left = REGS[Rs];
                            switch ((code >> 11) & L2)
                            {
                                // Format 1: move shifted register
                                case 0: // :00000
                                    // LSL Rd, Rs, #Offset5
                                    right = (code >> 6) & L5; // right = 0 ~ 31

                                    uleft = (uint) left;
                                    uvalue = uleft << right;
                                    value = (int) uvalue;
                                    
                                    if (right > 0)
                                        c = ((uleft << (right - 1)) & FN) != 0;
                                    break;
                                case 1: // :00001
                                    //  LSR Rd, Rs, #Offset5
                                    right = (code >> 6) & L5; // right = 1 ~ 32
                                    if (right == 0)
                                    {
                                        value = 0;
                                        c = (left & FN) != 0;
                                    }
                                    else
                                    {
                                        uleft = (uint) left;
                                        uvalue = uleft >> right;
                                        value = (int) uvalue;
                                        c = (left & (1 << (right - 1))) != 0;                                        
                                    }
                                    break;
                                case 2: // :00010
                                    // ASR Rd, Rs, #Offset5
                                    right = (code >> 6) & L5; // right = 1 ~ 32
                                    if (right == 0)
                                    {
                                        value = left >> 31 >> 1;
                                        c = (left & FN) != 0;
                                    }
                                    else
                                    {
                                        value = left >> right;
                                        c = (left & (1 << (right - 1))) != 0;
                                    }
                                    break;
                                case 3: // :00011
                                    // Format 2: add/subtract
                                    var I = ((code >> 10) & 0b1) != 0;
                                    var Rn = (code >> 6) & L3;
                                    Rs = (code >> 3) & L3;
                                    Rd = code & L3;
                                    left = REGS[Rs];
                                    right = I ? Rn : REGS[Rn];

                                    switch ((code >> 9) & L1)
                                    {
                                        case 0: // :0001100 | :0001110
                                            // ADD Rd, Rs, Rn
                                            // ADD Rd, Rs, #Offset3
                                            lvalue = Add(left, right);
                                            value = (int) lvalue;
                                            SetC(lvalue);
                                            SetV_Add(value, left, right);
                                            break;
                                        case 1: // :0001101 | :0001111
                                            // SUB Rd, Rs, Rn
                                            // SUB Rd, Rs, #Offset3
                                            lvalue = Sub(left, right);
                                            value = (int) lvalue;
                                            SetC(lvalue);
                                            SetV_Sub(value, left, right);
                                            break;
                                        default:
                                            throw new UnexceptedLogic();
                                    }

                                    break;
                                default:
                                    throw new UnexceptedLogic();
                            }

                            SetNZ(value);
                            REGS[Rd] = value;
                            break;
                        case 0b0010:
                        case 0b0011: // :001
                            // Format 3: move/compare/add/subtract immediate
                            Rd = (code >> 8) & L3;
                            left = REGS[Rd];
                            right = code & L8;
                            switch ((code >> 11) & L2)
                            {
                                case 0: // :001100
                                    // MOV Rd, #Offset8
                                    value = right;
                                    REGS[Rd] = value;
                                    break;
                                case 1: // :001101
                                    // CMP Rd, #Offset8
                                    lvalue = Sub(left, right);
                                    value = (int) lvalue;
                                    // only compare (no write)
                                    SetC(lvalue);
                                    SetV_Sub(value, left, right);
                                    break;
                                case 2: // :001110
                                    // ADD Rd, #Offset8
                                    lvalue = Add(left, right);
                                    value = (int) lvalue;
                                    REGS[Rd] = value;
                                    SetC(lvalue);
                                    SetV_Add(value, left, right);
                                    break;
                                case 3: // :001111
                                    // SUB Rd, #Offset8
                                    lvalue = Sub(left, right);
                                    value = (int) lvalue;
                                    REGS[Rd] = value;
                                    SetC(lvalue);
                                    SetV_Sub(value, left, right);
                                    break;
                                default:
                                    throw new UnexceptedLogic();
                            }

                            SetNZ(value);
                            break;
                        case 0b0100: // :0100
                            switch ((code >> 10) & L2)
                            {
                                case 0b00: // :010000
                                    // Format 4: ALU operations
                                    Rs = (code >> 3) & L3;
                                    Rd = code & L3;
                                    left = REGS[Rd];
                                    right = REGS[Rs];
                                    switch ((code >> 6) & L4)
                                    {
                                        case 0b0000: // :0100000000
                                            // AND Rd, Rs
                                            // Rd:= Rd AND Rs
                                            value = left & right;
                                            REGS[Rd] = value;
                                            break;
                                        case 0b0001: // :0100000001
                                            // EOR Rd, Rs
                                            // Rd:= Rd EOR Rs
                                            value = left ^ right;
                                            REGS[Rd] = value;
                                            break;
                                        case 0b0010: // :0100000010
                                            // LSL Rd, Rs
                                            // Rd := Rd << Rs
                                            
                                            if (right >= 32)
                                            {
                                                value = 0;
                                                c = right == 32 && (left & 1) != 0;
                                            }
                                            else if (right < 0)
                                            {
                                                value = 0;
                                                c = false;
                                            }
                                            else if (right == 0)
                                            {
                                                value = left;
                                            }
                                            else
                                            {
                                                uleft = (uint) left;
                                                uvalue = uleft << right;
                                                value = (int) uvalue;
                                                c = ((uleft << (right - 1)) & FN) != 0;
                                            }

                                            REGS[Rd] = value;
                                            break;
                                        case 0b0011: // :0100000011
                                            // LSR Rd, Rs
                                            // Rd := Rd >> Rs
                                            
                                            if (right >= 32)
                                            {
                                                value = 0;
                                                c = right == 32 && (left & FN) != 0;
                                            }
                                            else if (right < 0)
                                            {
                                                value = 0;
                                                c = false;
                                            }
                                            else if (right == 0)
                                            {
                                                value = left;
                                            }
                                            else
                                            {
                                                uleft = (uint) left;
                                                uvalue = uleft >> right;
                                                value = (int) uvalue;
                                                REGS[Rd] = value;
                                                c = ((uleft >> (right - 1)) & 1) != 0;
                                            }
                                            
                                            break;
                                        case 0b0100: // :0100000100
                                            // ASR Rd, Rs
                                            // Rd := Rd ASR Rs

                                            if (right < 0 || right >= 32)
                                            {
                                                value = left > 0? 0: -1;
                                                c = value < 0;
                                            }
                                            else if (right == 0)
                                            {
                                                value = left;
                                            }
                                            else
                                            {
                                                value = left >> right;
                                                REGS[Rd] = value;
                                                c = (left & (1 << (right - 1))) != 0;
                                            }
                                            
                                            break;
                                        case 0b0101: // :0100000101
                                            // ADC Rd, Rs
                                            // Rd := Rd + Rs + C-bit
                                            lvalue = Add(left, right) + (c ? 1UL : 0UL);
                                            value = (int) lvalue;
                                            REGS[Rd] = value;

                                            c = lvalue != (uint) value;
                                            v = left > 0 && right > 0 && value < 0 ||
                                                left < 0 && right < 0 && value > 0;
                                            break;
                                        case 0b0110: // :0100000110
                                            // SBC Rd, Rs
                                            // Rd := Rd - Rs - NOT C-bit
                                            var signed = (long) left - right - (c ? 0L : 1L);
                                            value = left - right - (c ? 0 : 1);
                                            REGS[Rd] = value;

                                            c = c || value < 0;
                                            v = signed != value;
                                            break;
                                        case 0b0111: // :0100000111
                                            // ROR Rd, Rs
                                            // Rd := Rd ROR Rs
                                            uleft = (uint) left;
                                            right &= 31;
                                            value = (int) ((uleft >> right) |
                                                           (uleft << (32 - right)));
                                            c = ((uleft >> (right - 1)) & I0) != 0;
                                            REGS[Rd] = value;

                                            break;
                                        case 0b1000: // :0100001000
                                            // TST Rd, Rs
                                            // Set condition codes on Rd AND Rs
                                            value = left & right;
                                            // only compare (no write)
                                            break;
                                        case 0b1001: // :0100001001
                                            // NEG Rd, Rs
                                            // Rd = -Rs
                                            lvalue = Sub(0, right);
                                            value = (int) lvalue;
                                            REGS[Rd] = value;
                                            SetC(lvalue);
                                            SetV_Sub(value, 0, right);
                                            break;
                                        case 0b1010: // :0100001010
                                            // CMP Rd, Rs
                                            // Set condition codes on Rd - Rs
                                            lvalue = Sub(left, right);
                                            value = (int) lvalue;
                                            // only compare (no write)
                                            SetC(lvalue);
                                            SetV_Sub(value, left, right);
                                            break;
                                        case 0b1011: // :0100001011
                                            // CMN Rd, Rs
                                            // Set condition codes on Rd + Rs
                                            lvalue = Add(left, right);
                                            value = (int) lvalue;
                                            // only compare (no write)
                                            SetC(lvalue);
                                            SetV_Add(value, left, right);
                                            break;
                                        case 0b1100: // :0100001100
                                            // ORR Rd, Rs
                                            // Rd := Rd OR Rs
                                            value = left | right;
                                            REGS[Rd] = value;
                                            break;
                                        case 0b1101: // :0100001101
                                            // MUL Rd, Rs
                                            // Rd := Rs * Rd
                                            long svalue = left * right;
                                            value = (int) svalue;
                                            REGS[Rd] = value;
                                            c |= value != svalue;
                                            v = false;
                                            // v = ((a ^ val) & ((~ b + 1) ^ val) & I31) != 0;
                                            break;
                                        case 0b1110: // :0100001110
                                            // BIC Rd, Rs
                                            // Rd := Rd AND NOT Rs
                                            value = left & ~right;
                                            REGS[Rd] = value;
                                            break;
                                        case 0b1111: // :0100001111
                                            // MVN Rd, Rs
                                            // Rd := NOT Rs
                                            value = ~right;
                                            REGS[Rd] = value;
                                            break;
                                        default:
                                            throw new UnexceptedLogic();
                                    }

                                    SetNZ(value);
                                    break;
                                case 0b01: // :010001
                                    // Format 5: Hi register operations/branch exchange
                                    var H1 = ((code >> 7) & L1) != 0;
                                    var H2 = ((code >> 6) & L1) != 0;
                                    Rd = (code & L3) + (H1 ? 8 : 0);
                                    Rs = ((code >> 3) & L3) + (H2 ? 8 : 0);

                                    switch ((code >> 8) & L2)
                                    {
                                        case 0b00: // :01000100
                                            // ADD Rd, Hs
                                            // ADD Hd, Rs
                                            // ADD Hd, Hs
                                            left = REGS[Rd];
                                            right = REGS[Rs];
                                            if (Rs == RegisterIndex.PC)
                                                right += 4;

                                            REGS[Rd] = left + right;
                                            // no write condition code flags
                                            break;
                                        case 0b01: // :01000101
                                            // CMP Rd, Hs
                                            // CMP Hd, Rs
                                            // CMP Hd, Hs
                                            left = REGS[Rd];
                                            right = REGS[Rs];
                                            lvalue = Sub(left, right);
                                            value = (int) lvalue;
                                            // only compare (no write)
                                            SetNZ(value);
                                            SetC(lvalue);
                                            SetV_Sub(value, left, right);
                                            break;
                                        case 0b10: // :01000110
                                            // MOV Rd, Hs
                                            // MOV Hd, Rs
                                            // MOV Hd, Hs
                                            value = REGS[Rs];
                                            if (Rd == RegisterIndex.PC)
                                                value -= 2;

                                            REGS[Rd] = value;
                                            break;
                                        case 0b11: // :01000111
                                            // BX Rs
                                            // BX Hs
                                            value = REGS[Rs];
                                            if ((value & I0) != 1)
                                                throw new UnknownInstructionException();

                                            if (H1)
                                                REGS[RegisterIndex.LR] = (REGS[RegisterIndex.PC] + 2) | I0;

                                            REGS[RegisterIndex.PC] = value & ~I0;
                                            incr_pc = false;
                                            break;
                                        default:
                                            throw new UnexceptedLogic();
                                    }

                                    break;
                                case 0b10:
                                case 0b11: // :01001
                                    // Format 6: PC-relative load
                                    // LDR Rd, [PC, #Imm]
                                    Rd = (code >> 8) & L3;
                                    addr = (code & L8) << 2;
                                    addr += (REGS[RegisterIndex.PC] + 4) & ~I1;

                                    REGS[Rd] = memory.ReadInt(addr);
                                    break;
                            }

                            break;
                        case 0b0101: // :0101
                            int Ro;
                            if ((code & I9) == 0) // :0101xx0
                            {
                                // Format 7: load/store with register offset
                                L = (code & I11) != 0;
                                B = (code & I10) != 0;
                                Ro = (code >> 6) & L3;
                                Rb = (code >> 3) & L3;
                                Rd = code & L3;
                                addr = REGS[Rb] + REGS[Ro];

                                if (L)
                                {
                                    if (B) // :0101110
                                    {
                                        // LDRB Rd, [Rb, Ro]
                                        REGS[Rd] = memory.ReadByte(addr);
                                    }
                                    else // :0101100
                                    {
                                        // LDR Rd, [Rb, Ro] 
                                        REGS[Rd] = memory.ReadInt(addr);
                                    }
                                }
                                else
                                {
                                    if (B) // :0101010
                                    {
                                        // STRB Rd, [Rb, Ro]
                                        memory.WriteByte(addr, (byte) REGS[Rd]);
                                    }
                                    else // :0101000
                                    {
                                        // STR Rd, [Rb, Ro]
                                        memory.WriteInt(addr, REGS[Rd]);
                                    }
                                }
                            }
                            else // :0101xx1
                            {
                                // Format 8: load/store sign-extended byte/halfword
                                H = (code & I11) != 0;
                                S = (code & I10) != 0;
                                Ro = (code >> 6) & L3;
                                Rb = (code >> 3) & L3;
                                Rd = code & L3;
                                addr = REGS[Rb] + REGS[Ro];

                                if (S)
                                {
                                    if (H) // :0101111
                                    {
                                        // LDSH Rd, [Rb, Ro]
                                        value = (short) memory.ReadShort(addr);
                                    }
                                    else // :0101011
                                    {
                                        // LDSB Rd, [Rb, Ro]
                                        value = (sbyte) memory.ReadByte(addr);
                                    }

                                    REGS[Rd] = value;
                                }
                                else
                                {
                                    if (H) // :0101101
                                    {
                                        // LDRH Rd, [Rb, Ro]
                                        value = memory.ReadShort(addr);
                                        REGS[Rd] = value;
                                    }
                                    else // :0101001
                                    {
                                        // STRH Rd, [Rb, Ro]
                                        value = REGS[Rd];
                                        memory.WriteShort(addr, (ushort) value);
                                    }
                                }
                            }

                            break;
                        case 0b0110:
                        case 0b0111: // :011
                            // Format 9: load/store with immediate offset
                            B = (code & I12) != 0;
                            L = (code & I11) != 0;
                            Rb = (code >> 3) & L3;
                            Rd = code & L3;
                            value = (code >> 6) & L5;
                            if (!B)
                                value <<= 2;

                            addr = REGS[Rb] + value;

                            if (L)
                            {
                                // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
                                if (!B) // :01111
                                    // LDR Rd, [Rb, #Imm]
                                    value = memory.ReadInt(addr);
                                else // :01101
                                    // LDRB Rd, [Rb, #Imm]
                                    value = memory.ReadByte(addr);

                                REGS[Rd] = value;
                            }
                            else
                            {
                                value = REGS[Rd];

                                if (!B) // :01100
                                    // STR Rd, [Rb, #Imm]
                                    memory.WriteInt(addr, value);
                                else // :01110
                                    // STRB Rd, [Rb, #Imm] 
                                    memory.WriteByte(addr, (byte) value);
                            }

                            break;
                        case 0b1000: // :1000x
                            // Format 10: load/store halfword
                            L = (code & I11) != 0;
                            Rb = (code >> 3) & L3;
                            Rd = code & L3;
                            value = ((code >> 6) & L5) << 1;
                            addr = REGS[Rb] + value;

                            if (L) // :10001
                                // LDRH Rd, [Rb, #Imm]
                                REGS[Rd] = memory.ReadShort(addr);
                            else // :10000
                                // STRH Rd, [Rb, #Imm]
                                memory.WriteShort(addr, (ushort) REGS[Rd]);

                            break;
                        case 0b1001: // :1001x
                            // Format 11: SP-relative load/store
                            L = (code & I11) != 0;
                            Rd = (code >> 8) & L3;
                            value = (code & L8) << 2;
                            addr = REGS[RegisterIndex.SP] + value;

                            if (L) // LDR:10011
                                // LDR Rd, [SP, #Imm]
                                REGS[Rd] = memory.ReadInt(addr);
                            else // STR:10010
                                // STR Rd, [SP, #Imm]
                                memory.WriteInt(addr, REGS[Rd]);

                            break;
                        case 0b1010: // :1010x
                            // Format 12: load address
                            var fSP = (code & I11) != 0;
                            Rd = (code >> 8) & L3;
                            value = (code & L8) << 2;

                            if (fSP) // :10101
                                // ADD Rd, SP, #Imm
                                value += REGS[RegisterIndex.SP];
                            else // :10100
                                // ADD Rd, PC, #Imm
                                value += (REGS[RegisterIndex.PC] + 4) & ~I1;

                            REGS[Rd] = value;
                            //  CPSR condition codes are unaffected
                            break;
                        case 0b1011: // :1011
                            bool R;
                            switch ((code >> 8) & L4)
                            {
                                case 0b0000: // :10110000x
                                    // Format 13: add offset to Stack Pointer

                                    S = (code & I7) != 0;
                                    value = (code & L7) << 2;

                                    if (S) // :101100000
                                        // ADD SP, #-Imm
                                        REGS[RegisterIndex.SP] -= value;
                                    else // :101100001
                                        // ADD SP, #Imm
                                        REGS[RegisterIndex.SP] += value;

                                    // condition codes are not set
                                    break;
                                case 0b0001: // :10110001
                                    // CBZ Rd, #Imm
                                    throw new NotImplementedException();
                                case 0b0010: // :10110010
                                    // SXTH, SXTB, UXTH, UXTB
                                    Rs = (code >> 3) & L3;
                                    Rd = code & L3;
                                    value = REGS[Rs];

                                    switch ((code >> 6) & L2)
                                    {
                                        case 0b00: // :1011001000
                                            // SXTH Rd, Rs
                                            value = (short) value;
                                            break;
                                        case 0b01: // :1011001001
                                            // SXTB Rd, Rs
                                            value = (sbyte) value;
                                            break;
                                        case 0b10: // :1011001010
                                            // UXTH Rd, Rs
                                            value = (ushort) value;
                                            break;
                                        case 0b11: // :1011001011
                                            // UXTB Rd, Rs
                                            value = (byte) value;
                                            break;
                                        default:
                                            throw new UnexceptedLogic();
                                    }

                                    REGS[Rd] = value;
                                    break;
                                case 0b0011: // :10110011
                                    // CBZ Rd, #Imm
                                    throw new NotImplementedException();
                                case 0b0100:
                                case 0b0101: // :1011010x
                                    // Format 14: push/pop registers
                                    R = (code & I8) != 0;
                                    list = code & L8;
                                    addr = REGS[RegisterIndex.SP];

                                    try
                                    {
                                        if (R) // :10110101
                                        {
                                            // PUSH { ..., LR }
                                            addr -= 4;
                                            memory.WriteInt(addr, REGS[RegisterIndex.LR]);
                                        }

                                        // PUSH { Rlist }
                                        for (var i = 7; i >= 0; i--)
                                        {
                                            if ((list & (1 << i)) != 0)
                                            {
                                                addr -= 4;
                                                memory.WriteInt(addr, REGS[i]);
                                            }
                                        }
                                    }
                                    finally
                                    {
                                        REGS[RegisterIndex.SP] = addr;
                                    }

                                    break;
                                case 0b0110: // :10110110
                                case 0b0111: // :10110111
                                case 0b1000: // :10111000
                                    throw new UnknownInstructionException();
                                case 0b1001: // :10111001
                                    // CBNZ Rd, #Imm
                                    throw new NotImplementedException();
                                case 0b1010: // :10111010xx
                                    Rs = (code >> 3) & L3;
                                    Rd = code & L3;
                                    value = REGS[Rs];

                                    switch ((code >> 6) & L2)
                                    {
                                        case 0b00: // :1011101000
                                            // REV Rd, Rs
                                            value = ((value >> 24) & 0xFF) |
                                                    ((value >> 16) & 0xFF) << 8 |
                                                    ((value >> 8) & 0xFF) << 16 |
                                                    ((value & 0xFF) << 24);
                                            break;
                                        case 0b01: // :1011101001
                                            // REV16 Rd, Rs
                                            throw new NotImplementedException();
                                        case 0b10: // :1011101010
                                            // INVALID
                                            throw new UnknownInstructionException();
                                        case 0b11: // :1011101011
                                            // REVSH Rd, Rs
                                            throw new NotImplementedException();
                                        default:
                                            throw new UnexceptedLogic();
                                    }

                                    REGS[Rd] = value;
                                    break;
                                case 0b1011: // :10111011
                                    // CBNZ Rd, #Imm
                                    throw new NotImplementedException();
                                case 0b1100:
                                case 0b1101: // :1011110x
                                    // Format 14: push/pop registers
                                    R = (code & I8) != 0;
                                    list = code & L8;
                                    addr = REGS[RegisterIndex.SP];

                                    try
                                    {
                                        // POP { Rlist }
                                        for (var i = 0; i < 8; i++)
                                        {
                                            if ((list & (1 << i)) != 0)
                                            {
                                                REGS[i] = memory.ReadInt(addr);
                                                addr += 4;
                                            }
                                        }

                                        if (R) // :10111101 {..., PC}
                                        {
                                            // POP { ..., PC }
                                            value = memory.ReadInt(addr);
                                            if ((value & I0) != 1)
                                                throw new InvalidAddressArmException();

                                            REGS[RegisterIndex.PC] = value & ~I0;
                                            addr += 4;
                                            incr_pc = false;
                                        }
                                    }
                                    finally
                                    {
                                        REGS[RegisterIndex.SP] = addr;
                                    }

                                    break;
                                case 0b1110: // :10111110
                                case 0b1111: // :10111111
                                    throw new UnknownInstructionException();
                                default:
                                    throw new UnexceptedLogic();
                            }

                            break;
                        case 0b1100: // :1100
                            // Format 15: multiple load/store
                            L = (code & I11) != 0;
                            list = code & L8;
                            Rb = (code >> 8) & L3;
                            addr = REGS[Rb];

                            try
                            {
                                if (!L) // :11001
                                {
                                    // STMIA Rb!, { Rlist }
                                    for (var i = 0; i < 8; i++)
                                        if ((list & (1 << i)) != 0)
                                        {
                                            memory.WriteInt(addr, REGS[i]);
                                            addr += 4;
                                        }
                                }
                                else // :11000
                                {
                                    // LDMIA Rb!, { Rlist }
                                    for (var i = 0; i < 8; i++)
                                        if ((list & (1 << i)) != 0)
                                        {
                                            REGS[i] = memory.ReadInt(addr);
                                            addr += 4;
                                        }
                                }
                            }
                            finally
                            {
                                REGS[Rb] = addr;
                            }

                            break;
                        case 0b1101: // :1101
                            var soffset = (byte) (code & L8);
                            var cond = false;

                            // Format 16: conditional branch
                            switch ((code >> 8) & L4)
                            {
                                case 0b0000: // :11010000
                                    // BEQ label
                                    cond = z;
                                    break;
                                case 0b0001: // :11010001
                                    // BNE label
                                    cond = !z;
                                    break;
                                case 0b0010: // :11010010
                                    // BCS label
                                    cond = c;
                                    break;
                                case 0b0011: // :11010011
                                    // BCC label
                                    cond = !c;
                                    break;
                                case 0b0100: // :11010100
                                    // BMI label
                                    cond = n;
                                    break;
                                case 0b0101: // :11010101
                                    // BPL label
                                    cond = !n;
                                    break;
                                case 0b0110: // :11010110
                                    // BVS label
                                    cond = v;
                                    break;
                                case 0b0111: // :11010111
                                    // BVC label
                                    cond = !v;
                                    break;
                                case 0b1000: // :11011000
                                    // BHI label
                                    cond = c && !z;
                                    break;
                                case 0b1001: // :11011001
                                    // BLS label
                                    cond = !c || z;
                                    break;
                                case 0b1010: // :11011010
                                    // BGE label
                                    cond = !(n ^ v);
                                    // = (n && v) || (!n && !v)
                                    break;
                                case 0b1011: // :11011011
                                    // BLT label
                                    cond = n ^ v;
                                    // = (n && !v) || (!n && v)
                                    break;
                                case 0b1100: // :11011100
                                    // BGT label
                                    cond = !z && !(n ^ v);
                                    // = !z && (n && v || !n && !v)
                                    break;
                                case 0b1101: // :11011101
                                    // BLE label
                                    cond = z || n ^ v;
                                    // = z || (n && !v) || (!n && v)
                                    break;
                                case 0b1110: // :11011110
                                    throw new UnknownInstructionException();
                                case 0b1111: // :11011111
                                    // Format 17: software interrupt
                                    // SWI Value8
                                    Interrupt(soffset);
                                    break;
                            }

                            if (cond)
                            {
                                value = (soffset & L8) << 1;
                                if ((value & I8) != 0)
                                    value |= -1 ^ L8;

                                REGS[RegisterIndex.PC] += 4 + value;
                                incr_pc = false;
                            }

                            break;
                        case 0b1110: // :11100
                            // Format 18: unconditional branch
                            if ((code & I11) != 0)
                                throw new UnknownInstructionException();

                            value = (code & L10) << 1;
                            if ((code & I10) != 0)
                                value |= -1 ^ L11;

                            REGS[RegisterIndex.PC] += 4 + value;
                            incr_pc = false;
                            break;
                        case 0b1111: // :1111
                            // Format 19: long branch with link
                            H = ((code >> 11) & L1) != 0;
                            value = code & L11;
                            if (!H)
                            {
                                REGS[RegisterIndex.LR] = value << 12;
                                count++;
                            }
                            else
                            {
                                addr = REGS[RegisterIndex.LR];
                                addr |= value << 1;
                                if ((addr & (1 << 22)) != 0)
                                {
                                    addr <<= 9;
                                    addr >>= 9;
                                }

                                var lr = REGS[RegisterIndex.PC];
                                REGS[RegisterIndex.PC] = lr + addr + 2;
                                REGS[RegisterIndex.LR] = lr + 3;
                                incr_pc = false;
                            }

                            break;
                        default:
                            throw new UnknownInstructionException();
                    }

                    if (incr_pc)
                        REGS[RegisterIndex.PC] += 2;
                }
            }
            finally
            {
                REGS[RegisterIndex.CPSR] = (q ? FQ : 0) |
                             (v ? FV : 0) |
                             (c ? FC : 0) |
                             (z ? FZ : 0) |
                             (n ? FN : 0);

                Regs.Store(REGS);
            }

            ulong Add(int a, int b)
                => (ulong) (uint) a + (uint) b;

            ulong Sub(int a, int b)
                => (ulong) (uint) a + ~ (uint) b + 1;

            void SetNZ(int val)
            {
                n = val < 0;
                z = val == 0;
            }

            void SetC(ulong lval)
                => c = lval > uint.MaxValue;

            void SetV_Add(int val, int a, int b)
                => v = ((a ^ val) & (b ^ val) & FN) != 0;

            void SetV_Sub(int val, int a, int b)
                => SetV_Add(val, a, ~b + 1);
        }
    }
}