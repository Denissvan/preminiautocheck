''轴参数初始化
''ECI-2600-1(ECI2600_1,192.168.0.12)
''Axios-0（左光源切换轴）Axios-1（右光源切换轴）Axios-2（左翻转轴）Axios-3（右翻转轴） 
BASE(0,1,2,3)
ATYPE=1,1,1,1
INVERT_STEP=256*100+6,256*100+6,256*100+6,256*100+6
UNITS=1,1,1,1
FS_LIMIT=10000000,10000000,10000000,10000000
RS_LIMIT=-10000000,-10000000,-10000000,-10000000
DATUM_IN=1,3,5,8
''原点
FWD_IN=2,4,7,10
INVERT_IN(1,OFF)
INVERT_IN(3,OFF)
INVERT_IN(5,OFF) 
INVERT_IN(8,OFF) 
''正限位
INVERT_IN(2,OFF)
INVERT_IN(4,OFF)
INVERT_IN(7,OFF) 
INVERT_IN(10,OFF) 





''负限位

REV_IN=-1,-1,6,9
INVERT_IN(6,OFF)
INVERT_IN(9,OFF)

''ALM_IN=24,25,26,27
''INVERT_IN(24,ON)
''INVERT_IN(25,ON)
''INVERT_IN(26,ON)
''INVERT_IN(27,ON)


'''''''''''''''''''光源控制
SETCOM(19200,8,1,0,0,0)
global dim sumchk	
global dim tempchar                    '接收的一个字节
global dim getbuff(20)                 '接收的支付串
global dim getnum                      '接收的字节数 
getnum = 0
global dim chset 
chset = 0 
global dim chcur 
chcur = 1 
runtask 0,get_char                    '启动接收字符串任务

''''''下位机特殊响应处理
while 1
	if IN(0)=OFF  THEN 			'如果(EMG)IN0被按下
		RAPIDSTOP(2)			'停止所有轴运动	
	ENDIF
	setch()
wend

'串口接收
global sub get_char()		 
	while 1
	GET #0,tempchar
	if tempchar = 76 then getnum = 0
	getbuff(getnum) = tempchar
	getnum = getnum + 1 
	if(getnum >=6 and getbuff(1)=71) then
		getnum =0				
		sumchk = getbuff(0)+getbuff(1)+getbuff(2)+getbuff(3)+getbuff(4)
		if sumchk = getbuff(5) then 
			chcur =getbuff(2)
			getbuff(5) =0
			TRACE chcur
		endif
	endif
	wend

end sub

global sub setch()
	if chcur <> chset then		
		PUTCHAR #0,76,66,0,1,0,143
		getnum =0	
		getbuff(5) =0
		if chset = 0 then 
			PUTCHAR #0,76,71,0,0,0,147
			endif
		if chset = 1 then 
			PUTCHAR #0,76,71,1,0,0,148
			endif
		if chset = 2 then 
			PUTCHAR #0,76,71,2,0,0,149
			endif
		if chset = 3 then 
			PUTCHAR #0,76,71,3,0,0,150
			endif
		DELAY(200)
	endif
end sub


end
