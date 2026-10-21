import math,random,zlib,struct
random.seed(7)
class Pic:
 def __init__(self,w,h,bg=(0,0,0,0)): self.w=w;self.h=h;self.p=bytearray(bytes(bg)*w*h)
 def rect(self,x,y,w,h,c):
  x=max(0,int(x));y=max(0,int(y));r=min(self.w,int(x+w));b=min(self.h,int(y+h))
  if r<=x or b<=y:return
  v=bytes(c)*(r-x)
  for j in range(y,b):self.p[(j*self.w+x)*4:(j*self.w+r)*4]=v
 def poly(self,pts,c):
  for y in range(max(0,int(min(p[1] for p in pts))),min(self.h,int(max(p[1] for p in pts))+1)):
   xs=[]
   for a,b in zip(pts,pts[1:]+pts[:1]):
    if (a[1]<=y<b[1]) or (b[1]<=y<a[1]):xs.append(a[0]+(y-a[1])*(b[0]-a[0])/(b[1]-a[1]))
   xs.sort()
   for a,b in zip(xs[::2],xs[1::2]):self.rect(a,y,b-a+1,1,c)
 def ellipse(self,x,y,w,h,c):
  for j in range(max(0,int(y)),min(self.h,int(y+h))):
   t=(j-y-h/2)/(h/2);dx=w/2*math.sqrt(max(0,1-t*t));self.rect(x+w/2-dx,j,2*dx+1,1,c)
 def line(self,x,y,xx,yy,c,t=1):
  n=max(abs(xx-x),abs(yy-y),1)
  for i in range(int(n)+1):self.rect(x+(xx-x)*i/n,y+(yy-y)*i/n,t,t,c)
 def save(self,name,grain=0):
  if grain:
   for i in range(0,len(self.p),4):
    if self.p[i+3]:
     g=random.randrange(-grain,grain+1)
     for k in range(3):self.p[i+k]=max(0,min(255,self.p[i+k]+g))
  def chunk(t,d):return struct.pack('!I',len(d))+t+d+struct.pack('!I',zlib.crc32(t+d)&0xffffffff)
  raw=b''.join(b'\0'+self.p[y*self.w*4:(y+1)*self.w*4] for y in range(self.h))
  open('Assets/Letters/Art/'+name+'.png','wb').write(b'\x89PNG\r\n\x1a\n'+chunk(b'IHDR',struct.pack('!2I5B',self.w,self.h,8,6,0,0,0))+chunk(b'IDAT',zlib.compress(raw))+chunk(b'IEND',b''))
def C(s):return tuple(bytes.fromhex(s))+(255,)
p=Pic(1600,1000,C('34312b'))
for y in range(1000):
 t=y/1000;p.rect(0,y,1600,1,(int(52+30*t),int(49+12*t),int(43-7*t),255))
# wooden panels
for x in range(0,1600,90):
 p.rect(x,0,3,650,C('242924'));p.rect(x+4,0,2,650,C('635542'))
p.rect(390,54,850,544,C('241f1a'));p.rect(407,65,816,513,C('746951'));p.rect(420,76,790,492,C('4c6470'))
for y in range(78,568):
 t=(y-78)/490;p.rect(420,y,790,1,(int(78-37*t),int(102-37*t),int(115-39*t),255))
# distant houses
for i in range(13):
 x=415+i*65;h=random.randint(100,225);y=552-h
 p.rect(x,y,66,h,C('364953'));p.poly([(x-8,y),(x+32,y-44),(x+73,y)],C('3b4b52'))
 for yy in range(y+30,540,48):
  for xx in range(x+12,x+60,28):p.rect(xx,yy,9,17,C(random.choice(['b19462','58666b','788077'])))
# cobbles street
for y in range(493,568,12):
 for x in range(420,1210,42):p.line(x+(y%24),y,x+28+(y%24),y,C('64767b'),2)
# rain etched
for i in range(240):
 x=random.randrange(425,1200);y=random.randrange(80,560);p.line(x,y,x-3,min(565,y+random.randint(8,28)),C('6f8790'))
for x in [420,678,947,1200]:p.rect(x,72,11,505,C('302f27'));p.rect(x+11,72,4,505,C('776c54'))
p.rect(420,268,790,10,C('31312a'));p.rect(420,278,790,3,C('7c7055'))
p.rect(390,578,855,20,C('87704e'));p.rect(386,597,865,14,C('29291f'))
# cubby shelf
p.rect(12,150,285,420,C('201f1b'))
for y in [150,250,350,450,550]:
 p.rect(12,y,285,12,C('725a40'));p.rect(12,y+12,285,7,C('3d3126'))
 for x in [22,118,214]:
  p.rect(x,y+24,72,53,C('9b8663'));p.poly([(x,y+24),(x+36,y+48),(x+72,y+24)],C('bcaa83'));p.line(x,y+76,x+36,y+48,C('766348'))
for x in [12,106,202,287]:p.rect(x,150,10,420,C('604c35'))
# notice board and clock
p.rect(1300,150,240,270,C('241f1b'));p.rect(1310,160,220,250,C('796c52'))
for x,y,w,h in [(1330,181,107,111),(1405,248,96,126)]:
 p.rect(x,y,w,h,C('bdad83'));p.ellipse(x+w/2-3,y+7,7,7,C('725041'))
 for yy in range(y+29,y+h-15,12):p.rect(x+12,yy,w-24,2,C('887959'))
p.ellipse(1346,27,130,130,C('201f1b'));p.ellipse(1353,34,116,116,C('bbae8b'));p.ellipse(1407,88,7,7,C('423e30'));p.line(1410,91,1410,48,C('423e30'),3);p.line(1410,91,1443,108,C('423e30'),3)
for i in range(12):
 a=i*math.pi/6;p.ellipse(1408+46*math.sin(a),89+46*math.cos(a),4,4,C('5c5743'))
# counter and desktop
p.rect(0,605,1600,40,C('8c6541'));p.rect(0,613,1600,5,C('bc8d57'));p.rect(0,645,1600,78,C('4b3626'));p.rect(0,715,1600,285,C('715037'))
for y in range(724,1000,54):
 p.line(0,y,1600,y,C('4b3929'),2);p.line(0,y+3,1600,y+3,C('84603e'),1)
for i in range(150):
 x=random.randrange(1600);y=random.randrange(725,1000);p.line(x,y,x+random.randrange(12,150),y,C('79583b'))
# left books and stationary
for i in range(4):
 p.poly([(0,654-i*18),(132,636-i*18),(205,672-i*18),(61,697-i*18)],C('b8a680'));p.poly([(0,654-i*18),(61,682-i*18),(61,697-i*18),(0,669-i*18)],C('796249'))
p.poly([(280,661),(345,661),(334,751),(290,751)],C('363f34'));p.ellipse(280,651,65,20,C('202b25'))
for i in range(5):p.line(291+i*10,670,280+i*15,590-random.randrange(30),C('9f8960'),5)
p.save('office',3)
# paper texture
p=Pic(600,800,C('e7d7b5'))
for i in range(1800):
 x=random.randrange(600);y=random.randrange(800);p.rect(x,y,random.randrange(1,4),1,C('d8c8a8'))
p.save('paper',2)
# portraits, expressive layered cut paper/chibi
styles=[('mother','a89a7c','687260',0),('husband','59483c','6d7373',1),('child','694b34','a18154',2),('father','b1a58b','565f50',3),('widow','50483f','4e5862',4),('lover','775b43','778173',5),('mira','594b40','64705f',6)]
for name,hair,coat,kind in styles:
 for mood in ['sad','soft','blink']:
  p=Pic(500,500);skin=C('d7aa7b');shade=C('bd8b64');hc=C(hair);cc=C(coat)
  # silhouette and shoulders
  p.ellipse(76,333,348,200,C('292e28'));p.poly([(138,336),(220,314),(288,314),(367,343),(421,499),(77,499)],cc)
  p.poly([(220,318),(247,379),(174,421),(154,351)],C('4a5448'));p.poly([(280,318),(247,379),(329,421),(347,351)],C('3d493f'))
  p.poly([(222,304),(281,304),(280,348),(250,372),(218,346)],shade)
  if kind==0:p.ellipse(157,15,141,128,hc)
  p.ellipse(96,66,310,293,hc)
  p.poly([(113,163),(146,110),(323,99),(384,175),(378,303),(329,347),(190,351),(130,309)],skin)
  p.poly([(133,176),(145,117),(228,84),(324,103),(379,164),(373,207),(335,183),(290,125),(224,154),(165,187),(124,222)],hc)
  p.poly([(96,157),(129,129),(143,242),(133,302),(106,273)],hc)
  p.poly([(372,155),(399,185),(399,279),(370,313),(351,293),(375,257)],hc)
  # hair facets
  def adjust(c,n):return tuple(max(0,min(255,v+n)) for v in c[:3])+(255,)
  p.poly([(144,123),(228,85),(299,100),(204,140),(130,191)],adjust(hc,15))
  p.poly([(299,100),(377,158),(388,216),(348,185)],adjust(hc,-13))
  # ears cheek
  p.ellipse(119,242,30,45,skin);p.ellipse(363,242,29,45,skin)
  p.ellipse(158,280,43,20,C('c89773'));p.ellipse(314,280,43,20,C('c89773'))
  if mood=='blink':
   p.line(176,249,198,251,C('3c382e'),4);p.line(310,251,332,249,C('3c382e'),4)
  else:
   p.ellipse(178,235,19,39,C('34362e'));p.ellipse(312,235,19,39,C('34362e'));p.rect(182,241,5,8,C('dcc4a0'));p.rect(316,241,5,8,C('dcc4a0'))
  if mood=='soft':
   p.line(173,217,200,213,C('6e5840'),4);p.line(305,213,334,217,C('6e5840'),4)
   p.line(241,303,253,309,C('89624b'),3);p.line(253,309,265,303,C('89624b'),3)
  else:
   p.line(173,220,199,209,C('6e5840'),4);p.line(306,209,333,220,C('6e5840'),4);p.line(245,307,260,306,C('89624b'),3)
  p.poly([(251,269),(246,285),(257,286)],C('be8962'))
  # hands hold envelope
  p.poly([(175,399),(326,399),(324,472),(178,472)],C('e3d2ac'));p.poly([(175,399),(251,445),(326,399)],C('f0e0b9'));p.line(178,470,234,431,C('b7a582'),2);p.line(324,470,272,432,C('b7a582'),2)
  p.ellipse(143,429,59,51,skin);p.ellipse(304,429,59,51,skin)
  if kind in [5,6]:
   p.rect(337,362,33,34,C('ddd4b7'));p.rect(350,367,8,24,C('9a6454'));p.rect(342,375,24,8,C('9a6454'))
  if kind==5:
   p.poly([(138,184),(170,172),(185,207),(150,221)],C('dfd6bc'));p.line(150,184,163,209,C('b5aa91'),2)
  if kind==6:
   p.ellipse(143,429,59,51,C('8d9993'))
   for xx in [151,163,175,187]:p.rect(xx,438,7,30,C('bec3b5'));p.rect(xx,450,7,4,C('56665f'))
  if kind==3:
   p.poly([(213,295),(251,289),(287,295),(268,307),(252,300),(235,307)],hc)
  if kind==2:
   p.poly([(100,158),(128,95),(206,75),(319,95),(378,154)],C('9d7851'));p.rect(97,151,287,23,C('715b42'))
  p.save(name+'-'+mood,2)
# mechanical foreground hand
p=Pic(320,300)
p.poly([(25,300),(74,196),(164,163),(242,191),(235,254),(211,300)],C('4a584c'))
p.poly([(76,207),(100,145),(189,120),(235,164),(211,226),(133,253)],C('7e8c87'))
p.poly([(93,201),(111,156),(177,140),(211,169),(192,215),(137,234)],C('acb3a3'))
for i in range(4):
 x=104+i*28;y=133-i*11
 p.poly([(x,y+27),(x-3,y-36),(x+10,y-55),(x+26,y-43),(x+24,y+17)],C('56665f'));p.rect(x+3,y-38,17,32,C('a3aea3'));p.rect(x+3,y,17,23,C('bbc0af'));p.ellipse(x+3,y-8,17,13,C('687971'))
p.poly([(210,190),(237,145),(270,129),(283,143),(266,169),(250,211)],C('a3aea3'));p.ellipse(124,229,31,24,C('4c5e58'));p.ellipse(130,234,18,14,C('bdc2ad'))
p.save('hand',2)
# newspaper line illustration of field hospital
p=Pic(600,210,C('d2c3a1'))
for x,y in [(65,80),(270,63),(440,96)]:
 p.poly([(x-40,y+80),(x+40,y-25),(x+115,y+80)],C('7c806e'));p.poly([(x+40,y-25),(x+164,y-14),(x+235,y+80),(x+115,y+80)],C('969580'));p.rect(x+23,y+37,38,43,C('525e55'));p.rect(x+112,y+20,35,28,C('d2c3a1'));p.rect(x+126,y+24,7,20,C('7e7863'));p.rect(x+119,y+31,21,6,C('7e7863'))
for i in range(150):
 x=random.randrange(600);y=random.randrange(180,210);p.line(x,y,x+20,y,C('9c9d86'))
p.save('hospital',2)
# An envelope with visible folds, used in every handling step.
p=Pic(500,280,C('dfcda7'))
p.poly([(0,0),(250,165),(499,0)],C('f0dfb9'))
p.poly([(0,279),(208,131),(250,165),(291,131),(499,279)],C('e8d6b0'))
p.line(0,0,250,165,C('af9a74'),2);p.line(250,165,499,0,C('af9a74'),2)
p.line(0,279,208,131,C('bba680'),2);p.line(499,279,291,131,C('bba680'),2)
p.save('envelope',2)
# Her right hand rests on the keys, alongside her articulated left.
p=Pic(320,300)
p.poly([(90,300),(101,225),(160,194),(244,209),(281,300)],C('4b5c4b'))
p.poly([(106,233),(98,172),(139,119),(212,133),(250,209),(239,251),(170,277)],C('d3a275'))
for x,y in [(111,141),(140,117),(168,111),(198,131)]:
 p.poly([(x,y+50),(x-9,y-24),(x-1,y-39),(x+14,y-39),(x+23,y-21),(x+27,y+48)],C('dcb083'))
 p.line(x-1,y+7,x+19,y+2,C('bf8e65'),2)
p.poly([(113,226),(75,186),(57,157),(67,145),(89,154),(135,186)],C('dfb184'))
p.save('right-hand',2)
# A transparent pool of amber light.
p=Pic(256,256)
for y in range(256):
 for x in range(256):
  a=max(0,1-math.sqrt(((x-128)/128)**2+((y-128)/128)**2))**2
  p.rect(x,y,1,1,(255,191,102,int(a*70)))
p.save('glow')
