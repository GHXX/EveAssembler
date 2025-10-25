    ldi r1, 46; current address
    ldi r2, 2047; max address to check (exclusive)
    ; ---- write data ----
writeLp:; write address to each cell
    sb r1, r1; sb rS2, rS1 - takes value of RS2 and stores at address RS1

; check/step loop counter
    inci r1, 2
    sub r0, r2, r1
    jgz writeLp; jump if r1 < r2

    ; ---- check data ----
    ldi r1, 46; current address
readLp:; read and check data from each cell
    lb r3, r1; read ram data

    ; if data does not match
    sub r0, r3, r1
    jnz failed

    ; check/step loop counter
    inci r1, 2
    sub r0, r2, r1
    jgz writeLp; jump if  r1 < r2            

    jmp ramOk

    ; ---- report result ----
failed:
    ldi r3, 0xFF
    jmp end
ramOk:
    ldi r3, 0x0F
    jmp end
end:hlt