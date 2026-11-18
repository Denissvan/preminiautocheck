''轴参数初始化
''2600-1扩展IO：
''OUT32 左工位气缸1上
''OUT33 左工位气缸1下
''OUT34 左工位气缸2上
''OUT35 左工位气缸2下
''OUT36 左工位气缸3上
''OUT37 左工位气缸3下
''OUT38 左工位气缸4上
''OUT39 左工位气缸4下
''OUT40 右工位气缸1上
''OUT41 右工位气缸1下
''OUT42 右工位气缸2上
''OUT43 右工位气缸2下
''OUT44 右工位气缸3上
''OUT45 右工位气缸3下
''OUT46 右工位气缸4上
''OUT47 右工位气缸4下


''IN32 左工位气缸1电磁阀上
''IN33 左工位气缸1电磁阀下
''IN34 左工位气缸2电磁阀上
''IN35 左工位气缸2电磁阀下
''IN36 左工位气缸3电磁阀上
''IN37 左工位气缸3电磁阀下
''IN38 左工位气缸4电磁阀上
''IN39 左工位气缸4电磁阀下
''IN40 右工位气缸1电磁阀上
''IN41 右工位气缸1电磁阀下
''IN42 右工位气缸2电磁阀上
''IN43 右工位气缸2电磁阀下
''IN44 右工位气缸3电磁阀上
''IN45 右工位气缸3电磁阀下
''IN46 右工位气缸4电磁阀上
''IN47 右工位气缸4电磁阀下

''ECI-2600-1(ECI2600_1,192.168.0.11)
''
''Axios-0（左前轴）Axios-1（左后轴）Axios-2（右前轴）Axios-3（右后轴） Axios-4（左X轴）Axios-5（右X轴）
BASE(0,1,2,3,4,5)
ATYPE=1,1,1,1,1,1
INVERT_STEP=256*100+6,256*100+6,256*100+6,256*100+6,256*100+6,256*100+6
UNITS=1,1,1,1,1,1
FS_LIMIT=10000000,10000000,10000000,10000000,10000000,10000000
RS_LIMIT=-10000000,-10000000,-10000000,-10000000,-10000000,-10000000
DATUM_IN=1,3,5,7,9,11

FWD_IN=-1,-1,-1,-1,10,12
INVERT_IN(1,OFF)
INVERT_IN(3,OFF)
INVERT_IN(5,OFF) 
INVERT_IN(7,OFF) 
INVERT_IN(9,OFF) 
INVERT_IN(11,OFF) 


REV_IN=2,4,6,8,-1,-1

INVERT_IN(2,OFF)
INVERT_IN(4,OFF)
INVERT_IN(6,OFF)
INVERT_IN(8,OFF)

INVERT_IN(10,OFF)
INVERT_IN(12,OFF)

ALM_IN=24,25,26,27,28,29
INVERT_IN(24,OFF)
INVERT_IN(25,OFF)
INVERT_IN(26,OFF)
INVERT_IN(27,OFF)
INVERT_IN(28,OFF)
INVERT_IN(29,OFF)

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
''while 1
	''if IN(0)=OFF  THEN 			'如果(EMG)IN0被按下
		''RAPIDSTOP(2)			'停止所有轴运动	
	''ENDIF
	''setch()
''wend

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
