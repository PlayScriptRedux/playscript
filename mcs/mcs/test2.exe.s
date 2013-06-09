.section __DWARF, __debug_abbrev,regular,debug

	.byte 1,17,1,37,8,3,8,27,8,19,11,17,1,18,1,16,6,0,0,2,46,1,3,8,90,8,17,1,18,1,64,10
	.byte 0,0,3,5,0,3,8,73,19,2,10,0,0,15,5,0,3,8,73,19,2,6,0,0,4,36,0,11,11,62,11,3
	.byte 8,0,0,5,2,1,3,8,11,15,0,0,17,2,0,3,8,11,15,0,0,6,13,0,3,8,73,19,56,10,0,0
	.byte 7,22,0,3,8,73,19,0,0,8,4,1,3,8,11,15,73,19,0,0,9,40,0,3,8,28,13,0,0,10,57,1
	.byte 3,8,0,0,11,52,0,3,8,73,19,2,10,0,0,12,52,0,3,8,73,19,2,6,0,0,13,15,0,73,19,0
	.byte 0,14,16,0,73,19,0,0,16,28,0,73,19,56,10,0,0,18,46,0,3,8,90,8,17,1,18,1,0,0,0
.section __DWARF, __debug_info,regular,debug
Ldebug_info_start:

LDIFF_SYM0=Ldebug_info_end - Ldebug_info_begin
	.long LDIFF_SYM0
Ldebug_info_begin:

	.short 2
	.long 0
	.byte 4,1
	.asciz "Mono AOT Compiler 3.0.10 ((no/eff4cb5 Sat Apr 13 19:24:30 EDT 2013)"
	.asciz "JITted code"
	.asciz ""

	.byte 2,0,0,0,0,0,0,0,0
LDIFF_SYM1=Ldebug_line_start - Ldebug_line_section_start
	.long LDIFF_SYM1
LDIE_I1:

	.byte 4,1,5
	.asciz "sbyte"
LDIE_U1:

	.byte 4,1,7
	.asciz "byte"
LDIE_I2:

	.byte 4,2,5
	.asciz "short"
LDIE_U2:

	.byte 4,2,7
	.asciz "ushort"
LDIE_I4:

	.byte 4,4,5
	.asciz "int"
LDIE_U4:

	.byte 4,4,7
	.asciz "uint"
LDIE_I8:

	.byte 4,8,5
	.asciz "long"
LDIE_U8:

	.byte 4,8,7
	.asciz "ulong"
LDIE_I:

	.byte 4,4,5
	.asciz "intptr"
LDIE_U:

	.byte 4,4,7
	.asciz "uintptr"
LDIE_R4:

	.byte 4,4,4
	.asciz "float"
LDIE_R8:

	.byte 4,8,4
	.asciz "double"
LDIE_BOOLEAN:

	.byte 4,1,2
	.asciz "boolean"
LDIE_CHAR:

	.byte 4,2,8
	.asciz "char"
LDIE_STRING:

	.byte 4,4,1
	.asciz "string"
LDIE_OBJECT:

	.byte 4,4,1
	.asciz "object"
LDIE_SZARRAY:

	.byte 4,4,1
	.asciz "object"
.section __DWARF, __debug_loc,regular,debug
Ldebug_loc_start:
.section __DWARF, __debug_frame,regular,debug
	.align 3

LDIFF_SYM2=Lcie0_end - Lcie0_start
	.long LDIFF_SYM2
Lcie0_start:

	.long -1
	.byte 3
	.asciz ""

	.byte 1,124,8,12,5,4,136,1
	.align 2
Lcie0_end:
.text
	.align 3
methods:
	.space 16
.text
	.align 4
L_m_0:
	.no_dead_strip _Test_Test__ctor
_Test_Test__ctor:

	.byte 85,139,236,83,131,236,4,232,0,0,0,0,91,129,195
	.long _mono_aot_test2_got - . + 3
	.byte 141,101,252,91,201,195

Lme_0:
.text
	.align 4
L_m_1:
	.no_dead_strip _Test_Test_Main
_Test_Test_Main:

	.byte 85,139,236,83,87,86,131,236,92,232,0,0,0,0,91,129,195
	.long _mono_aot_test2_got - . + 3
	.byte 139,131
	.long 16
	.byte 131,236,12,80,232
	.long _p_1 - . -4
	.byte 131,196,16,137,69,208,139,131
	.long 20
	.byte 131,236,8,106,100,80,232
	.long _p_2 - . -4
	.byte 131,196,16,137,69,204,139,131
	.long 20
	.byte 131,236,8,106,100,80,232
	.long _p_2 - . -4
	.byte 131,196,16,137,69,200,199,69,196,0,0,0,0,235,35,141,164,36,0,0,0,0,139,69,204,139,77,196,57,72,12,15
	.byte 134,41,5,0,0,141,68,136,16,137,8,139,69,196,64,137,69,196,131,125,196,100,124,222,131,236,12,255,117,208,139,69
	.byte 208,57,0,232
	.long _p_3 - . -4
	.byte 131,196,16,131,236,12,255,117,208,139,69,208,57,0,232
	.long _p_4 - . -4
	.byte 131,196,16,199,69,228,0,0,0,0,235,61,141,164,36,0,0,0,0,51,246,235,41,141,100,36,0,139,69,204,57,112
	.byte 12,15,134,197,4,0,0,141,68,176,16,139,8,139,69,200,57,112,12,15,134,172,4,0,0,141,68,176,16,137,8,70
	.byte 131,254,100,124,214,131,69,228,1,129,125,228,128,150,152,0,124,193,131,236,12,255,117,208,139,69,208,57,0,232
	.long _p_5 - . -4
	.byte 131,196,16,139,131
	.long 24
	.byte 131,236,8,106,4,80,232
	.long _p_2 - . -4
	.byte 131,196,16,139,200,137,77,164,139,139
	.long 28
	.byte 131,236,4,81,106,0,80,139,0,255,144,128,0,0,0,131,196,16,139,69,164,137,69,168,137,69,172,131,236,12,255,117
	.byte 208,139,69,208,57,0,232
	.long _p_6 - . -4
	.byte 131,196,16,137,85,192,137,69,188,139,131
	.long 32
	.byte 131,236,12,80,232
	.long _p_7 - . -4
	.byte 131,196,16,139,200,139,69,168,139,85,192,137,81,12,139,85,188,137,81,8,131,236,4,81,106,1,80,139,0,255,144,128
	.byte 0,0,0,131,196,16,139,69,172,139,200,137,77,176,139,139
	.long 36
	.byte 131,236,4,81,106,2,80,139,0,255,144,128,0,0,0,131,196,16,139,69,176,137,69,180,137,69,184,131,236,12,255,117
	.byte 208,139,69,208,57,0,232
	.long _p_8 - . -4
	.byte 131,196,16,137,85,192,137,69,188,139,131
	.long 32
	.byte 131,236,12,80,232
	.long _p_7 - . -4
	.byte 131,196,16,139,200,139,69,180,139,85,192,137,81,12,139,85,188,137,81,8,131,236,4,81,106,3,80,139,0,255,144,128
	.byte 0,0,0,131,196,16,139,69,184,131,236,12,80,232
	.long _p_9 - . -4
	.byte 131,196,16,131,236,12,80,232
	.long _p_10 - . -4
	.byte 131,196,16,139,69,204,131,120,12,0,15,134,82,3,0,0,5,16,0,0,0,137,69,224,139,69,200,131,120,12,0,15
	.byte 134,46,3,0,0,5,16,0,0,0,137,69,220,131,236,12,255,117,208,139,69,208,57,0,232
	.long _p_3 - . -4
	.byte 131,196,16,131,236,12,255,117,208,139,69,208,57,0,232
	.long _p_4 - . -4
	.byte 131,196,16,199,69,216,0,0,0,0,235,46,141,109,0,51,255,235,27,141,100,36,0,139,69,220,3,199,139,77,224,3
	.byte 207,139,81,4,139,9,137,80,4,137,8,131,199,8,129,255,144,1,0,0,124,225,131,69,216,1,129,125,216,128,150,152
	.byte 0,124,204,131,236,12,255,117,208,139,69,208,57,0,232
	.long _p_5 - . -4
	.byte 131,196,16,139,131
	.long 24
	.byte 131,236,8,106,4,80,232
	.long _p_2 - . -4
	.byte 131,196,16,139,200,137,77,164,139,139
	.long 40
	.byte 131,236,4,81,106,0,80,139,0,255,144,128,0,0,0,131,196,16,139,69,164,137,69,168,137,69,172,131,236,12,255,117
	.byte 208,139,69,208,57,0,232
	.long _p_6 - . -4
	.byte 131,196,16,137,85,192,137,69,188,139,131
	.long 32
	.byte 131,236,12,80,232
	.long _p_7 - . -4
	.byte 131,196,16,139,200,139,69,168,139,85,192,137,81,12,139,85,188,137,81,8,131,236,4,81,106,1,80,139,0,255,144,128
	.byte 0,0,0,131,196,16,139,69,172,139,200,137,77,176,139,139
	.long 36
	.byte 131,236,4,81,106,2,80,139,0,255,144,128,0,0,0,131,196,16,139,69,176,137,69,180,137,69,184,131,236,12,255,117
	.byte 208,139,69,208,57,0,232
	.long _p_8 - . -4
	.byte 131,196,16,137,85,192,137,69,188,139,131
	.long 32
	.byte 131,236,12,80,232
	.long _p_7 - . -4
	.byte 131,196,16,139,200,139,69,180,139,85,192,137,81,12,139,85,188,137,81,8,131,236,4,81,106,3,80,139,0,255,144,128
	.byte 0,0,0,131,196,16,139,69,184,131,236,12,80,232
	.long _p_9 - . -4
	.byte 131,196,16,131,236,12,80,232
	.long _p_10 - . -4
	.byte 131,196,16,131,236,12,255,117,208,139,69,208,57,0,232
	.long _p_3 - . -4
	.byte 131,196,16,131,236,12,255,117,208,139,69,208,57,0,232
	.long _p_4 - . -4
	.byte 131,196,16,199,69,212,0,0,0,0,235,23,131,236,4,106,100,255,117,200,255,117,204,232
	.long _p_11 - . -4
	.byte 131,196,16,131,69,212,1,129,125,212,128,150,152,0,124,224,131,236,12,255,117,208,139,69,208,57,0,232
	.long _p_5 - . -4
	.byte 131,196,16,139,131
	.long 24
	.byte 131,236,8,106,4,80,232
	.long _p_2 - . -4
	.byte 131,196,16,139,200,137,77,164,139,139
	.long 44
	.byte 131,236,4,81,106,0,80,139,0,255,144,128,0,0,0,131,196,16,139,69,164,137,69,168,137,69,172,131,236,12,255,117
	.byte 208,139,69,208,57,0,232
	.long _p_6 - . -4
	.byte 131,196,16,137,85,192,137,69,188,139,131
	.long 32
	.byte 131,236,12,80,232
	.long _p_7 - . -4
	.byte 131,196,16,139,200,139,69,168,139,85,192,137,81,12,139,85,188,137,81,8,131,236,4,81,106,1,80,139,0,255,144,128
	.byte 0,0,0,131,196,16,139,69,172,139,200,137,77,176,139,139
	.long 36
	.byte 131,236,4,81,106,2,80,139,0,255,144,128,0,0,0,131,196,16,139,69,176,137,69,180,137,69,184,131,236,12,255,117
	.byte 208,139,69,208,57,0,232
	.long _p_8 - . -4
	.byte 131,196,16,137,85,192,137,69,188,139,131
	.long 32
	.byte 131,236,12,80,232
	.long _p_7 - . -4
	.byte 131,196,16,139,200,139,69,180,139,85,192,137,81,12,139,85,188,137,81,8,131,236,4,81,106,3,80,139,0,255,144,128
	.byte 0,0,0,131,196,16,139,69,184,131,236,12,80,232
	.long _p_9 - . -4
	.byte 131,196,16,131,236,12,80,232
	.long _p_10 - . -4
	.byte 131,196,16,141,69,232,131,236,12,80,232
	.long _p_12 - . -4
	.byte 131,196,12,141,101,244,94,95,91,201,195,104,67,3,0,0,104,20,1,0,0,232
	.long _p_13 - . -4
	.byte 104,88,3,0,0,235,239,104,171,4,0,0,235,232,104,189,4,0,0,235,225,104,26,5,0,0,235,218

Lme_1:
.text
	.align 3
methods_end:
.text
	.align 3
code_offsets:

LDIFF_SYM3=L_m_0 - methods
	.long LDIFF_SYM3
LDIFF_SYM4=L_m_1 - methods
	.long LDIFF_SYM4
	.long -1

.text
	.align 3
unbox_trampolines:
unbox_trampolines_end:
.text
	.align 3
method_info_offsets:

	.long 3,10,1,2
	.short 0
	.byte 1,2,255,255,255,255,253
.text
	.align 3
extra_method_table:

	.long 11,0,0,0,0,0,0,0
	.long 0,0,0,0,0,0,0,0
	.long 0,0,0,0,0,0,0,0
	.long 0,0,0,0,0,0,0,0
	.long 0,0
.text
	.align 3
extra_method_info_offsets:

	.long 0
.text
	.align 3
class_name_table:

	.short 11, 1, 0, 0, 0, 0, 0, 0
	.short 0, 0, 0, 0, 0, 0, 0, 2
	.short 0, 0, 0, 0, 0, 0, 0
.text
	.align 3
got_info_offsets:

	.long 12,10,2,2
	.short 0, 10
	.byte 23,2,1,1,1,5,7,7,3,5,58,3
.text
	.align 3
ex_info_offsets:

	.long 3,10,1,2
	.short 0
	.byte 128,226,5,255,255,255,255,25
.text
	.align 3
unwind_info:

	.byte 16,12,5,4,136,1,65,14,8,132,2,66,13,4,65,131,3,22,12,5,4,136,1,65,14,8,132,2,66,13,4,65
	.byte 131,3,65,135,4,65,134,5
.text
	.align 3
class_info_offsets:

	.long 2,10,1,2
	.short 0
	.byte 128,234,7

.text
	.align 4
plt:
_mono_aot_test2_plt:
	.no_dead_strip plt__jit_icall_mono_object_new_ptrfree
plt__jit_icall_mono_object_new_ptrfree:
_p_1:

	.byte 255,163
	.long 52,64
	.no_dead_strip plt__jit_icall_mono_array_new_specific
plt__jit_icall_mono_array_new_specific:
_p_2:

	.byte 255,163
	.long 56,90
	.no_dead_strip plt_System_Diagnostics_Stopwatch_Restart
plt_System_Diagnostics_Stopwatch_Restart:
_p_3:

	.byte 255,163
	.long 60,116
	.no_dead_strip plt_System_Diagnostics_Stopwatch_Start
plt_System_Diagnostics_Stopwatch_Start:
_p_4:

	.byte 255,163
	.long 64,121
	.no_dead_strip plt_System_Diagnostics_Stopwatch_Stop
plt_System_Diagnostics_Stopwatch_Stop:
_p_5:

	.byte 255,163
	.long 68,126
	.no_dead_strip plt_System_Diagnostics_Stopwatch_get_ElapsedMilliseconds
plt_System_Diagnostics_Stopwatch_get_ElapsedMilliseconds:
_p_6:

	.byte 255,163
	.long 72,131
	.no_dead_strip plt__jit_icall_mono_object_new_ptrfree_box
plt__jit_icall_mono_object_new_ptrfree_box:
_p_7:

	.byte 255,163
	.long 76,136
	.no_dead_strip plt_System_Diagnostics_Stopwatch_get_ElapsedTicks
plt_System_Diagnostics_Stopwatch_get_ElapsedTicks:
_p_8:

	.byte 255,163
	.long 80,166
	.no_dead_strip plt_string_Concat_object__
plt_string_Concat_object__:
_p_9:

	.byte 255,163
	.long 84,171
	.no_dead_strip plt_System_Console_WriteLine_string
plt_System_Console_WriteLine_string:
_p_10:

	.byte 255,163
	.long 88,176
	.no_dead_strip plt_System_Array_Copy_System_Array_System_Array_int
plt_System_Array_Copy_System_Array_System_Array_int:
_p_11:

	.byte 255,163
	.long 92,181
	.no_dead_strip plt_System_Console_ReadKey
plt_System_Console_ReadKey:
_p_12:

	.byte 255,163
	.long 96,186
	.no_dead_strip plt__jit_icall_mono_arch_throw_corlib_exception
plt__jit_icall_mono_arch_throw_corlib_exception:
_p_13:

	.byte 255,163
	.long 100,191
plt_end:
.text
	.align 3
image_table:

	.long 3
	.asciz "test2"
	.asciz "ACBDD922-2DE6-46C3-9A17-6A9905A49C07"
	.asciz ""
	.asciz ""
	.align 3

	.long 0,0,0,0,0
	.asciz "System"
	.asciz "621AE06A-2342-49DE-86F6-20A7F82574F2"
	.asciz ""
	.asciz "b77a5c561934e089"
	.align 3

	.long 1,4,0,0,0
	.asciz "mscorlib"
	.asciz "AB835E1D-03EB-4980-A7A5-BA593388BD24"
	.asciz ""
	.asciz "b77a5c561934e089"
	.align 3

	.long 1,4,0,0,0
.data
	.align 3
_mono_aot_test2_got:
	.space 104
got_end:
.text
	.align 2
assembly_guid:
	.asciz "ACBDD922-2DE6-46C3-9A17-6A9905A49C07"
.text
	.align 2
runtime_version:
	.asciz ""
.text
	.align 2
assembly_name:
	.asciz "test2"
.data
	.align 3
_mono_aot_file_info:
	.globl _mono_aot_file_info

	.long 88,0
	.align 2
	.long _mono_aot_test2_got
	.align 2
	.long methods
	.align 2
	.long 0
	.align 2
	.long blob
	.align 2
	.long class_name_table
	.align 2
	.long class_info_offsets
	.align 2
	.long method_info_offsets
	.align 2
	.long ex_info_offsets
	.align 2
	.long code_offsets
	.align 2
	.long extra_method_info_offsets
	.align 2
	.long extra_method_table
	.align 2
	.long got_info_offsets
	.align 2
	.long methods_end
	.align 2
	.long unwind_info
	.align 2
	.long mem_end
	.align 2
	.long image_table
	.align 2
	.long plt
	.align 2
	.long plt_end
	.align 2
	.long assembly_guid
	.align 2
	.long runtime_version
	.align 2
	.long 0
	.align 2
	.long 0
	.align 2
	.long 0
	.align 2
	.long 0
	.align 2
	.long 0
	.align 2
	.long 0
	.align 2
	.long assembly_name
	.align 2
	.long unbox_trampolines
	.align 2
	.long unbox_trampolines_end

	.long 12,104,14,3,0,110193151,63,264
	.long 0,0,0,0,0,0,0,0
	.long 0,0,0,0,128,8,8
.text
	.align 3
blob:

	.byte 0,0,0,0,18,4,5,5,6,7,8,9,8,6,10,8,9,8,6,11,8,9,8,12,0,39,42,46,14,2,130,131
	.byte 1,14,6,1,2,129,24,2,14,6,1,2,129,66,2,17,0,1,14,2,129,25,2,17,0,35,17,0,39,17,0,71
	.byte 7,23,109,111,110,111,95,111,98,106,101,99,116,95,110,101,119,95,112,116,114,102,114,101,101,0,7,23,109,111,110,111
	.byte 95,97,114,114,97,121,95,110,101,119,95,115,112,101,99,105,102,105,99,0,3,193,0,22,128,3,193,0,22,126,3,193
	.byte 0,22,127,3,193,0,22,120,7,27,109,111,110,111,95,111,98,106,101,99,116,95,110,101,119,95,112,116,114,102,114,101
	.byte 101,95,98,111,120,0,3,193,0,22,121,3,194,0,17,223,3,194,0,9,22,3,194,0,7,84,3,194,0,9,38,7
	.byte 32,109,111,110,111,95,97,114,99,104,95,116,104,114,111,119,95,99,111,114,108,105,98,95,101,120,99,101,112,116,105,111
	.byte 110,0,128,130,0,16,0,2,17,0,0,128,144,8,0,0,1,4,128,128,8,0,0,1,194,0,16,141,194,0,16,138
	.byte 194,0,16,137,194,0,16,135,98,111,101,104,109,0
.section __DWARF, __debug_info,regular,debug
LTDIE_1:

	.byte 17
	.asciz "System_Object"

	.byte 8,7
	.asciz "System_Object"

LDIFF_SYM5=LTDIE_1 - Ldebug_info_start
	.long LDIFF_SYM5
LTDIE_1_POINTER:

	.byte 13
LDIFF_SYM6=LTDIE_1 - Ldebug_info_start
	.long LDIFF_SYM6
LTDIE_1_REFERENCE:

	.byte 14
LDIFF_SYM7=LTDIE_1 - Ldebug_info_start
	.long LDIFF_SYM7
LTDIE_0:

	.byte 5
	.asciz "Test_Test"

	.byte 8,16
LDIFF_SYM8=LTDIE_1 - Ldebug_info_start
	.long LDIFF_SYM8
	.byte 2,35,0,0,7
	.asciz "Test_Test"

LDIFF_SYM9=LTDIE_0 - Ldebug_info_start
	.long LDIFF_SYM9
LTDIE_0_POINTER:

	.byte 13
LDIFF_SYM10=LTDIE_0 - Ldebug_info_start
	.long LDIFF_SYM10
LTDIE_0_REFERENCE:

	.byte 14
LDIFF_SYM11=LTDIE_0 - Ldebug_info_start
	.long LDIFF_SYM11
	.byte 2
	.asciz "Test.Test:.ctor"
	.asciz "Test.Test:.ctor"
	.long L_m_0
	.long Lme_0

	.byte 2,118,16,3
	.asciz "this"

LDIFF_SYM12=LDIE_I4 - Ldebug_info_start
	.long LDIFF_SYM12
	.byte 0,0

.section __DWARF, __debug_frame,regular,debug

LDIFF_SYM13=Lfde0_end - Lfde0_start
	.long LDIFF_SYM13
Lfde0_start:

	.long 0
	.align 2
	.long L_m_0

LDIFF_SYM14=Lme_0 - L_m_0
	.long LDIFF_SYM14
	.byte 65,14,8,132,2,66,13,4,65,131,3
	.align 2
Lfde0_end:

.section __DWARF, __debug_info,regular,debug
LTDIE_4:

	.byte 5
	.asciz "System_ValueType"

	.byte 8,16
LDIFF_SYM15=LTDIE_1 - Ldebug_info_start
	.long LDIFF_SYM15
	.byte 2,35,0,0,7
	.asciz "System_ValueType"

LDIFF_SYM16=LTDIE_4 - Ldebug_info_start
	.long LDIFF_SYM16
LTDIE_4_POINTER:

	.byte 13
LDIFF_SYM17=LTDIE_4 - Ldebug_info_start
	.long LDIFF_SYM17
LTDIE_4_REFERENCE:

	.byte 14
LDIFF_SYM18=LTDIE_4 - Ldebug_info_start
	.long LDIFF_SYM18
LTDIE_3:

	.byte 5
	.asciz "System_Int64"

	.byte 16,16
LDIFF_SYM19=LTDIE_4 - Ldebug_info_start
	.long LDIFF_SYM19
	.byte 2,35,0,6
	.asciz "m_value"

LDIFF_SYM20=LDIE_I8 - Ldebug_info_start
	.long LDIFF_SYM20
	.byte 2,35,8,0,7
	.asciz "System_Int64"

LDIFF_SYM21=LTDIE_3 - Ldebug_info_start
	.long LDIFF_SYM21
LTDIE_3_POINTER:

	.byte 13
LDIFF_SYM22=LTDIE_3 - Ldebug_info_start
	.long LDIFF_SYM22
LTDIE_3_REFERENCE:

	.byte 14
LDIFF_SYM23=LTDIE_3 - Ldebug_info_start
	.long LDIFF_SYM23
LTDIE_5:

	.byte 5
	.asciz "System_Boolean"

	.byte 9,16
LDIFF_SYM24=LTDIE_4 - Ldebug_info_start
	.long LDIFF_SYM24
	.byte 2,35,0,6
	.asciz "m_value"

LDIFF_SYM25=LDIE_BOOLEAN - Ldebug_info_start
	.long LDIFF_SYM25
	.byte 2,35,8,0,7
	.asciz "System_Boolean"

LDIFF_SYM26=LTDIE_5 - Ldebug_info_start
	.long LDIFF_SYM26
LTDIE_5_POINTER:

	.byte 13
LDIFF_SYM27=LTDIE_5 - Ldebug_info_start
	.long LDIFF_SYM27
LTDIE_5_REFERENCE:

	.byte 14
LDIFF_SYM28=LTDIE_5 - Ldebug_info_start
	.long LDIFF_SYM28
LTDIE_2:

	.byte 5
	.asciz "System_Diagnostics_Stopwatch"

	.byte 28,16
LDIFF_SYM29=LTDIE_1 - Ldebug_info_start
	.long LDIFF_SYM29
	.byte 2,35,0,6
	.asciz "elapsed"

LDIFF_SYM30=LDIE_I8 - Ldebug_info_start
	.long LDIFF_SYM30
	.byte 2,35,8,6
	.asciz "started"

LDIFF_SYM31=LDIE_I8 - Ldebug_info_start
	.long LDIFF_SYM31
	.byte 2,35,16,6
	.asciz "is_running"

LDIFF_SYM32=LDIE_BOOLEAN - Ldebug_info_start
	.long LDIFF_SYM32
	.byte 2,35,24,0,7
	.asciz "System_Diagnostics_Stopwatch"

LDIFF_SYM33=LTDIE_2 - Ldebug_info_start
	.long LDIFF_SYM33
LTDIE_2_POINTER:

	.byte 13
LDIFF_SYM34=LTDIE_2 - Ldebug_info_start
	.long LDIFF_SYM34
LTDIE_2_REFERENCE:

	.byte 14
LDIFF_SYM35=LTDIE_2 - Ldebug_info_start
	.long LDIFF_SYM35
LTDIE_6:

	.byte 5
	.asciz "System_Int32"

	.byte 12,16
LDIFF_SYM36=LTDIE_4 - Ldebug_info_start
	.long LDIFF_SYM36
	.byte 2,35,0,6
	.asciz "m_value"

LDIFF_SYM37=LDIE_I4 - Ldebug_info_start
	.long LDIFF_SYM37
	.byte 2,35,8,0,7
	.asciz "System_Int32"

LDIFF_SYM38=LTDIE_6 - Ldebug_info_start
	.long LDIFF_SYM38
LTDIE_6_POINTER:

	.byte 13
LDIFF_SYM39=LTDIE_6 - Ldebug_info_start
	.long LDIFF_SYM39
LTDIE_6_REFERENCE:

	.byte 14
LDIFF_SYM40=LTDIE_6 - Ldebug_info_start
	.long LDIFF_SYM40
	.byte 2
	.asciz "Test.Test:Main"
	.asciz "Test.Test:Main"
	.long L_m_1
	.long Lme_1

	.byte 2,118,16,11
	.asciz "V_0"

LDIFF_SYM41=LTDIE_2_REFERENCE - Ldebug_info_start
	.long LDIFF_SYM41
	.byte 2,116,80,11
	.asciz "V_1"

LDIFF_SYM42=LDIE_SZARRAY - Ldebug_info_start
	.long LDIFF_SYM42
	.byte 2,116,76,11
	.asciz "V_2"

LDIFF_SYM43=LDIE_SZARRAY - Ldebug_info_start
	.long LDIFF_SYM43
	.byte 2,116,72,11
	.asciz "V_3"

LDIFF_SYM44=LDIE_I4 - Ldebug_info_start
	.long LDIFF_SYM44
	.byte 2,116,68,11
	.asciz "V_4"

LDIFF_SYM45=LDIE_I4 - Ldebug_info_start
	.long LDIFF_SYM45
	.byte 2,116,100,11
	.asciz "V_5"

LDIFF_SYM46=LDIE_I4 - Ldebug_info_start
	.long LDIFF_SYM46
	.byte 1,86,11
	.asciz "V_6"

LDIFF_SYM47=LDIE_I - Ldebug_info_start
	.long LDIFF_SYM47
	.byte 2,116,96,11
	.asciz "V_7"

LDIFF_SYM48=LDIE_I - Ldebug_info_start
	.long LDIFF_SYM48
	.byte 2,116,92,11
	.asciz "V_8"

LDIFF_SYM49=LDIE_I4 - Ldebug_info_start
	.long LDIFF_SYM49
	.byte 2,116,88,11
	.asciz "V_9"

LDIFF_SYM50=LDIE_I4 - Ldebug_info_start
	.long LDIFF_SYM50
	.byte 1,87,11
	.asciz "V_10"

LDIFF_SYM51=LDIE_I4 - Ldebug_info_start
	.long LDIFF_SYM51
	.byte 2,116,84,0

.section __DWARF, __debug_frame,regular,debug

LDIFF_SYM52=Lfde1_end - Lfde1_start
	.long LDIFF_SYM52
Lfde1_start:

	.long 0
	.align 2
	.long L_m_1

LDIFF_SYM53=Lme_1 - L_m_1
	.long LDIFF_SYM53
	.byte 65,14,8,132,2,66,13,4,65,131,3,65,135,4,65,134,5
	.align 2
Lfde1_end:

.section __DWARF, __debug_info,regular,debug

	.byte 0
Ldebug_info_end:
.section __DWARF, __debug_line,regular,debug
Ldebug_line_section_start:
Ldebug_line_start:

	.long Ldebug_line_end - . -4
	.short 2
	.long Ldebug_line_header_end - . -4
	.byte 1,1,251,14,13,0,1,1,1,1,0,0,0,1,0,0,1
.section __DWARF, __debug_line,regular,debug

	.byte 0
	.asciz "<unknown>"

	.byte 0,0,0,0
Ldebug_line_header_end:

	.byte 0,1,1
Ldebug_line_end:
.text
	.align 3
mem_end:
