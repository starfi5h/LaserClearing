# LaserClearing

![demo](https://raw.githubusercontent.com/starfi5h/LaserClearing/dev/img/demo.gif)  
Use mecha laser to auto-harvest trees and stones in range.  
The toggle button is right next to the battery bar.  
This mod is inspired by GreyHak's [DSP Drone Clearing](https://dsp.thunderstore.io/package/GreyHak/DSP_Drone_Clearing/).  

## Configuration
Run the game one time to generate `BepInEx\config\starfi5h.plugin.LaserClearing.cfg` file.  
配置文件(.cfg)需要先运行过游戏一次才会出现。修改后需重启游戏才会生效。    

```
## Settings file was created by plugin LaserClearing v1.0.1
## Plugin GUID: starfi5h.plugin.LaserClearing

[General]

## Enable LaserClearing
# Setting type: Boolean
# Default value: true
Enable = true

## Get drops from destroying trees and stones
## 破坏的树木/石头时获取掉落物
# Setting type: Boolean
# Default value: true
EnableLoot = true

[Laser]

## Maximum count of laser
## 激光最大数量
# Setting type: Int32
# Default value: 3
MaxCount = 3

## Maximum range of laser
## 激光最远距离
# Setting type: Single
# Default value: 40
Range = 40

## Time to mine an object (tick)
## 开采所需时间
# Setting type: Int32
# Default value: 60
MiningTick = 60

## Interval to check objects in range
## 检查周期
# Setting type: Int32
# Default value: 20
CheckIntervalTick = 20

## Power consumption  per laser (kW)
## 激光耗能
# Setting type: Single
# Default value: 480
MiningPower = 480

[Target]

## Targets only objects with available drop
## 只清除有掉落物的植被
# Setting type: Boolean
# Default value: true
DropOnly = true
```

