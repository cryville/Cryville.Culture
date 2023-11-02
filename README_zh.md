[English](README.md)

# Cryville.Culture
**本项目目前处于开发中状态，不建议用于生产环境。**

项目 #A016 [ccl] Cryville.Culture 是一个 .NET 下用于处理区域性信息的库，可以解析 [Unicode 通用区域设置数据储存库（CLDR）](https://cldr.unicode.org/)中的 Unicode 通用区域设置数据，并提供利用这些数据的方法。其中包括部分在 [Unicode 区域设置数据标记语言（LDML）标准](https://unicode.org/reports/tr35/)中描述的算法。

## 支持的数据和功能
### 核心
- Unicode 语言和区域设置标识符
  - [x] Unicode 语言标识符
    - [x] 标准化
  - [ ] Unicode 区域设置标识符
  - [ ] BCP 47 语言标签转换
  - [x] 有效性数据
- 区域设置继承和匹配
  - [x] 子标签倾向
  - [x] 语言匹配
    - [x] 增强语言匹配
