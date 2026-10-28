# 언덕 마을 약도 (이미지 AI에 배치 참고로 첨부). 사용법: python3 scripts/town_layout.py → docs/art/town-layout.png(글자 없음), town-layout-labeled.png(이름표)
from PIL import Image, ImageDraw, ImageFont
W,H=941,1672
SEA=(70,140,205); SEA2=(95,165,225); LAND=(150,190,120); TREE=(95,150,85); PATH=(232,215,180); PLAZA=(226,208,170)
WALL=(165,160,150); BLUE=(60,100,200); ORANGE=(215,120,70); TEAL=(70,160,150); HANOK=(70,70,80); WOOD=(170,120,70)
def base():
    im=Image.new('RGB',(W,H),LAND); d=ImageDraw.Draw(im)
    # 하늘·먼 산·섬
    d.rectangle([0,0,W,70],fill=(180,215,240))
    d.polygon([(0,70),(150,40),(330,75),(520,35),(700,60),(941,45),(941,110),(0,110)],fill=(120,160,130))
    # 오른쪽 위 바다와 섬
    d.polygon([(560,110),(941,90),(941,470),(860,420),(760,330),(700,200)],fill=SEA)
    for cx,cy,rx,ry in [(780,170,70,22),(880,280,50,18),(640,230,60,20)]: d.ellipse([cx-rx,cy-ry,cx+rx,cy+ry],fill=TREE)
    # 아래 바다
    d.polygon([(0,1170),(941,1170),(941,H),(0,H)],fill=SEA)
    for y in range(1220,H,70):
        for x in range(30,W,160): d.line([(x,y),(x+50,y)],fill=SEA2,width=4)
    # 숲 덩어리
    for cx,cy,r in [(60,180,70),(250,140,60),(640,120,60),(90,480,60),(870,560,70),(60,820,50),(900,820,50),(620,540,50),(260,400,40)]:
        d.ellipse([cx-r,cy-r*0.7,cx+r,cy+r*0.7],fill=TREE)
    # 광장
    d.polygon([(150,880),(700,880),(940,980),(940,1170),(0,1170),(0,1120),(150,1120)],fill=PLAZA)
    # 부두 벽과 작은 잔교
    d.rectangle([0,1160,830,1200],fill=WALL)
    d.rectangle([330,1200,420,1260],fill=WALL)
    # S자 길 (아래 넓고 위로 갈수록 조금 좁게)
    pts=[(480,900),(430,780),(450,650),(520,540),(500,450),(420,370),(430,290),(470,210),(450,150)]
    widths=[150,140,130,115,105,95,85,75,65]
    for (p,q),w in zip(zip(pts,pts[1:]),widths):
        d.line([p,q],fill=PATH,width=w)
        d.ellipse([q[0]-w/2,q[1]-w/2,q[0]+w/2,q[1]+w/2],fill=PATH)
    d.ellipse([480-75,900-75,480+75,900+75],fill=PLAZA)
    # 전망대: 벤치와 큰 나무
    d.ellipse([300,70,440,150],fill=TREE); d.rectangle([365,130,380,165],fill=WOOD)
    d.rectangle([420,140,470,152],fill=WOOD)
    # 돌담
    for seg in [[(0,520),(360,560)],[(330,850),(420,860)],[(640,800),(941,820)],[(700,900),(941,920)]]:
        d.line(seg,fill=WALL,width=14)
    return im,d
def house(d,x0,y0,x1,y1,roof,door=None):
    d.rectangle([x0,y0+(y1-y0)*0.45,x1,y1],fill=(240,235,225),outline=(120,110,100),width=2)
    d.polygon([(x0-8,y0+(y1-y0)*0.48),(x0+15,y0),(x1-15,y0),(x1+8,y0+(y1-y0)*0.48)],fill=roof,outline=(60,60,70))
    if door: d.rectangle(door,fill=(230,120,150))
def draw():
    im,d=base()
    # 위쪽 집들
    house(d,55,215,150,265,ORANGE); house(d,175,195,300,265,BLUE); house(d,300,215,370,265,ORANGE)
    house(d,140,295,285,355,TEAL); house(d,460,260,570,320,BLUE); house(d,540,315,640,375,(235,190,190),door=(548,348,562,375))
    d.line([(600,345),(700,345)],fill=(250,250,250),width=3)  # 빨래줄
    house(d,0,370,105,420,BLUE); house(d,95,415,275,520,ORANGE); house(d,245,460,430,560,BLUE)
    house(d,630,435,750,500,BLUE); house(d,760,440,860,510,ORANGE); house(d,840,600,941,640,BLUE)
    # 미끼가게 (파란 지붕, 초록·흰 줄무늬 차양)
    house(d,10,590,330,820,BLUE)
    for i in range(6): d.rectangle([255+i*17,665,272+i*17,720],fill=(60,170,140) if i%2==0 else (245,245,240))
    # 할머니댁 한옥
    d.rectangle([570,690,925,800],fill=WOOD,outline=(90,60,40),width=3)
    d.polygon([(555,700),(610,590),(900,590),(940,700)],fill=HANOK)
    d.rectangle([580,700,620,760],fill=(120,80,40))  # 문
    # 위판장
    d.polygon([(0,885),(240,885),(310,950),(300,970),(0,970)],fill=BLUE)
    d.rectangle([0,970,300,1120],fill=(235,230,215),outline=(110,110,110),width=2)
    for i in range(8): d.rectangle([20+i*34,1050,48+i*34,1100],fill=(70,120,210) if i%2 else (230,190,70))
    # 푸드트럭
    d.rectangle([700,965,915,1075],fill=(250,250,250),outline=(40,90,170),width=4)
    d.rectangle([700,1040,915,1075],fill=(60,110,190))
    for i in range(7): d.rectangle([730+i*26,975,756+i*26,1000],fill=(60,110,190) if i%2==0 else (250,250,250))
    for x in (720,880): d.ellipse([x-18,1070,x+18,1106],fill=(40,40,45))
    # 배 둘
    for x0,y0,x1,y1 in [(40,1265,285,1335),(385,1310,625,1390)]:
        d.polygon([(x0,y0+20),(x1,y0+20),(x1-30,y1),(x0+25,y1)],fill=(250,250,250),outline=(180,70,60))
        d.rectangle([x0+90,y0-15,x0+150,y0+20],fill=(240,240,240),outline=(120,120,120))
    # 방파제: 광장 오른쪽 끝에서 아래로 뻗고 끝에 빨간 등대
    d.polygon([(760,1170),(860,1170),(870,1470),(770,1470)],fill=WALL)
    d.line([(760,1170),(770,1470)],fill=(90,90,95),width=4); d.line([(860,1170),(870,1470)],fill=(90,90,95),width=4)
    for y in range(1185,1480,34):
        for x in (880,915,945):
            d.ellipse([x-17+((y//34)%2)*10,y-15,x+17+((y//34)%2)*10,y+15],fill=(150,150,150),outline=(110,110,110))
    d.ellipse([755,1440,885,1520],fill=WALL,outline=(90,90,95),width=3)
    d.rectangle([800,1360,840,1480],fill=(210,50,50),outline=(120,30,30),width=3)
    d.rectangle([792,1350,848,1362],fill=(250,250,250))
    d.polygon([(800,1350),(840,1350),(820,1325)],fill=(150,40,40))
    # 가로등 (광장·부두·길 아래쪽, 지금 그림과 같은 자리)
    for x,y in [(295,860),(125,1150),(650,1185),(800,880),(310,330)]:
        d.line([(x,y),(x,y-70)],fill=(80,80,85),width=6); d.ellipse([x-13,y-85,x+13,y-59],fill=(255,220,120))
    return im
im=draw()
out='docs/art/'
im.save(out+'town-layout.png')
lab=im.copy(); d=ImageDraw.Draw(lab)
f=ImageFont.truetype('godot/fonts/GowunBatang-Bold.ttf',30)
for t,x,y in [('전망대',330,165),('민박',560,385),('미끼가게',60,740),('할머니댁',640,720),('S자 길',470,600),('광장',420,1020),('위판장',60,990),('여울호 트럭',720,1110),('부두',150,1210),('배',120,1350),('방파제',650,1260),('등대',690,1400),('가로등',230,770)]:
    bb=d.textbbox((x,y),t,font=f); d.rectangle([bb[0]-6,bb[1]-4,bb[2]+6,bb[3]+4],fill=(30,38,50)); d.text((x,y),t,font=f,fill=(255,225,140))
lab.save(out+'town-layout-labeled.png')
