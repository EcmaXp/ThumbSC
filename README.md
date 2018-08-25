# ThumbSC
Thumb Simulator (C#)

## Reference
- https://github.com/SolraBizna/jarm
- https://github.com/katsuster/ememu
- https://ece.uwaterloo.ca/~ece222/ARM/ARM7-TDMI-manual-pt3.pdf
- https://static.docs.arm.com/ddi0419/d/DDI0419D_armv6m_arm.pdf

## Limitation
- It was used as a kind of stepping stone to implement ThumbSJ.
- Some instructions are not implemented properly or are incorrect.
- Interrupts are not supported because isc vectors are not supported. (SVC instruction is supported)
- Only application registers are supported.
