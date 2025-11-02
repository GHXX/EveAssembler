; r1 current address
            ldi r0, 0x2000; read start address
            lb r1, r0

            ; r2 max address to check (exclusive)
            ldi r2, 4095

            ; r3 read data
            ; r4 output
            ldi r4, 0

            ; ---- check cell ----
        checkLp:
            sb r1, r1 ; store r1 in r1
            lb r3, r1 ; r3=ram[r1]

            ; if data does not match
            sub r0, r3, r1
            jnz failed
        
            inci r1, 2
            sub r0, r2, r1
            jgz checkLp; jump if r1 < r2

            jmp ramOk
            ; ---- report result ----
        failed:
            ldi r4, 0xFFFF
            jmp end
        ramOk:
            ldi r4, 0x00FF
        end:hlt