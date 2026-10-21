# Reuse the established procedural portrait construction without regenerating
# existing art. The cap, shoulder tabs and worn coat identify the new customer.
from pathlib import Path
source=Path('Assets/Letters/Art/draw_art.py').read_text()
base=source[:source.index("p=Pic(1600")]
portraits=source[source.index('for name,hair,coat,kind in styles:'):source.index('# mechanical foreground hand')]
portraits=portraits.replace("  p.save(name+'-'+mood,2)","""  p.poly([(103,153),(123,83),(185,63),(328,75),(384,119),(380,160)],C('68705a'))
  p.poly([(107,143),(376,143),(398,171),(313,181),(104,168)],C('434e40'))
  p.rect(123,130,247,10,C('85866c'))
  p.rect(237,98,24,28,C('b2a17c'));p.rect(243,104,12,16,C('737962'))
  p.poly([(125,348),(164,341),(183,359),(139,371)],C('a29b77'))
  p.poly([(337,343),(372,354),(362,374),(322,359)],C('a29b77'))
  p.line(280,372,340,417,C('414b3d'),9)
  p.rect(343,386,24,18,C('b6ac8b'))
  p.line(120,471,137,457,C('8a8970'),3)
  p.save(name+'-'+mood,2)""")
exec(base+"styles=[('soldier','51483b','68705a',1)]\n"+portraits)
