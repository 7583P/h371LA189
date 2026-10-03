import math
control=[(0,6,0),(0,6,110),(25,7,215),(85,8,290),(175,9,320),(260,11,285),(310,13,210),(315,19,135),(285,11,55),(235,8,-15),(195,7,-85),(255,6,-165),(220,6,-250),(125,7,-280),(45,9,-235),(-45,11,-250),(-120,10,-180),(-100,8,-90),(-35,6,-60),(0,6,-40)]
add=lambda a,b:tuple(x+y for x,y in zip(a,b))
sub=lambda a,b:tuple(x-y for x,y in zip(a,b))
mul=lambda a,s:tuple(x*s for x in a)
length=lambda a:sum(x*x for x in a)**.5
norm=lambda a:mul(a,1/length(a))
nodes=[]
for i,b in enumerate(control):
 a,c,d=control[i-1],control[(i+1)%20],control[(i+2)%20]
 for s in range(8):
  t=s/8
  nodes.append(tuple(.5*(2*b[k]+(-a[k]+c[k])*t+(2*a[k]-5*b[k]+4*c[k]-d[k])*t*t+(-a[k]+3*b[k]-3*c[k]+d[k])*t*t*t) for k in range(3)))
for name,en,ex in [('RIDGE',88,104),('SKY',26,38),('WATERFALL',10,22),('APEX',115,134)]:
 f=norm(sub(nodes[(en+1)%160],nodes[en-1])); ef=norm(sub(nodes[(ex+1)%160],nodes[ex-1]));chord=sub(nodes[ex],nodes[en]);ln=length(chord)
 print(name,'start',tuple(round(x,1) for x in nodes[en]),'end',tuple(round(x,1) for x in nodes[ex]),'f',tuple(round(x,2) for x in f),'endf',tuple(round(x,2) for x in ef),'chord',round(ln,1),'main',round(sum(length(sub(nodes[i+1],nodes[i]))for i in range(en,ex)),1),'center',round(ln*.44,1))
for en,ex in [(74,110),(82,110),(80,108),(25,46),(50,70),(113,134),(88,104),(10,22),(8,22),(2,22)]:
 chord=length(sub(nodes[ex],nodes[en]));main=sum(length(sub(nodes[i+1],nodes[i]))for i in range(en,ex))
 print(en,ex,'main',round(main,1),'chord',round(chord,1),'save',round(main-chord,1))
def path_for(en,ex):
 start,end=nodes[en],nodes[ex];chord=sub(end,start);direction=norm(chord);handle=min(34,max(18,length(chord)*.18));first=add(start,mul(chord,.28));last=add(start,mul(chord,.72));f=norm(sub(nodes[en+1],nodes[en-1]));ef=norm(sub(nodes[ex+1],nodes[ex-1]));arr=[None]*25
 def bezier(a,b,c,d,t):
  u=1-t
  return add(add(mul(a,u**3),mul(b,3*u*u*t)),add(mul(c,3*u*t*t),mul(d,t**3)))
 for i in range(9):
  t=i/8
  arr[i]=bezier(start,add(start,mul(f,handle)),sub(first,mul(direction,handle)),first,t)
  arr[16+i]=bezier(last,add(last,mul(direction,handle)),sub(end,mul(ef,handle)),end,t)
  arr[8+i]=add(first,mul(sub(last,first),t))
 return arr
for name,en,ex in [('RIDGE',82,110),('SKY',50,70),('WATERFALL',25,46),('APEX',113,134)]:
 pp=path_for(en,ex)
 angles=[]
 for i in range(23):
  a,b=sub(pp[i+1],pp[i]),sub(pp[i+2],pp[i+1]);angles.append(math.degrees(math.acos(max(-1,min(1,sum(x*y for x,y in zip(norm(a),norm(b))))))))
 print(name,'length',round(sum(length(sub(pp[i+1],pp[i]))for i in range(24)),1),'largestTurn',round(max(angles),1),'at',angles.index(max(angles)))
for en,ex in [(82,110),(80,110),(81,110),(83,110),(84,110),(85,110),(86,110),(88,110)]:
 ch=sub(nodes[ex],nodes[en]);f=norm(sub(nodes[en+1],nodes[en-1]));ang=math.degrees(math.acos(sum(x*y for x,y in zip(f,norm(ch)))))
 print('entry',en,'head',tuple(round(v,2)for v in f),'angle',round(ang,1),'save',round(sum(length(sub(nodes[i+1],nodes[i]))for i in range(en,ex))-length(ch),1))
