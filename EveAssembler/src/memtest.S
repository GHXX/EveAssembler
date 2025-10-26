            ldi r1, 50; current address
            ldi r2, 4095; max address to check (exclusive)
            ldi r4, 0
            ; ---- write data ----
        writeLp:; write address to each cell
            sw r1, r1; sb rS2, rS1 - takes value of RS2 and stores at address RS1

        ; check/step loop counter
            inci r1, 2
            sub r0, r2, r1
            jgz writeLp; jump if r1 < r2

            ; ---- check data ----
            ldi r1, 50; current address
        readLp:; read and check data from each cell
            lw r3, r1; read ram data

            ; if data does not match
            sub r0, r3, r1
            jnz failed

            ; check/step loop counter
            inci r1, 2
            sub r0, r2, r1
            jgz readLp; jump if  r1 < r2            

            jmp ramOk

            ; ---- report result ----
        failed:
            ldi r4, 0xFFFF
            jmp end
        ramOk:
            ldi r4, 0x00FF
            jmp end
        end:hlt