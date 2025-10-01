# TreeHelper 树形数据辅助类

`TreeHelper` 是一个功能强大的树形数据操作工具类，提供了平面数据与树形数据之间的相互转换功能，以及树形数据的遍历操作。**无需实现特定接口，完全兼容现有代码**。

## 功能特性

- ✅ **平面数据转树形数据** - 将扁平的父子关系数据转换为树形结构
- ✅ **树形数据转平面数据** - 将树形结构数据转换为扁平列表
- ✅ **树形数据遍历** - 非递归的前序遍历算法
- ✅ **泛型支持** - 支持 int、string、Guid 等各种类型的节点ID
- ✅ **高性能** - 使用字典查找，时间复杂度 O(n)
- ✅ **非递归实现** - 避免栈溢出，适合深层树结构
- ✅ **完全兼容** - 无需实现接口，适配任何现有类
- ✅ **完整测试** - 100% 单元测试覆盖

## 核心方法

### 1. ToTree<T, TKey> - 平面数据转树形数据

将具有父子关系的平面数据转换为树形结构。

```csharp
public static IList<T> ToTree<T, TKey>(
    IEnumerable<T> flatData,
    Func<T, TKey> getId,
    Func<T, TKey> getParentId,
    Func<T, IList<T>> getChildren,
    Action<T, IList<T>> setChildren,
    TKey rootId = default(TKey))
```

**参数说明：**
- `flatData`: 平面数据列表
- `getId`: 获取节点ID的委托函数
- `getParentId`: 获取父节点ID的委托函数
- `getChildren`: 获取子节点集合的委托函数
- `setChildren`: 设置子节点集合的委托函数
- `rootId`: 根节点ID，默认为 `default(TKey)`

**返回值：**
- 树形数据列表（根节点集合）

### 2. ToFlat<T> - 树形数据转平面数据

将树形结构数据转换为扁平列表，支持深度优先遍历。

```csharp
public static IList<T> ToFlat<T>(
    IEnumerable<T> treeData,
    Func<T, IEnumerable<T>> getChildren,
    Func<T, T> createFlatNode)
```

**参数说明：**
- `treeData`: 树形数据列表
- `getChildren`: 获取子节点集合的委托函数
- `createFlatNode`: 创建平面节点的委托函数

**返回值：**
- 平面数据列表

### 3. TraverseWithStack<T> - 树形数据遍历

使用堆栈实现树的前序遍历（非递归）。

```csharp
public static void TraverseWithStack<T>(
    T root,
    Func<T, IEnumerable<T>> getChildNodes,
    Action<T> processNode)
```

**参数说明：**
- `root`: 根节点
- `getChildNodes`: 获取节点子节点的委托函数
- `processNode`: 处理节点的委托函数

## 使用示例

### 基本用法

```csharp
// 1. 定义树节点类（无需实现任何接口）
public class TreeNode
{
    public int Id { get; set; }
    public int ParentId { get; set; }
    public string Name { get; set; }
    public IList<TreeNode> Children { get; set; } = new List<TreeNode>();
}

// 2. 创建平面数据
var flatData = new List<TreeNode>
{
    new TreeNode { Id = 1, ParentId = 0, Name = "根节点1" },
    new TreeNode { Id = 2, ParentId = 0, Name = "根节点2" },
    new TreeNode { Id = 3, ParentId = 1, Name = "子节点1-1" },
    new TreeNode { Id = 4, ParentId = 1, Name = "子节点1-2" },
    new TreeNode { Id = 5, ParentId = 2, Name = "子节点2-1" }
};

// 3. 平面数据转树形数据
var treeData = TreeHelper.ToTree(
    flatData,
    x => x.Id,           // 获取ID
    x => x.ParentId,     // 获取父ID
    x => x.Children,     // 获取子节点集合
    (x, children) => x.Children = children,  // 设置子节点集合
    0                    // 根节点ID
);

// 4. 树形数据转平面数据
var flatResult = TreeHelper.ToFlat(
    treeData,
    x => x.Children,     // 获取子节点集合
    x => new TreeNode    // 创建平面节点
    {
        Id = x.Id,
        ParentId = x.ParentId,
        Name = x.Name,
        Children = null
    }
);

// 5. 树形数据遍历
TreeHelper.TraverseWithStack(
    treeData.First(),
    x => x.Children,
    x => Console.WriteLine($"处理节点: {x.Name}")
);
```

### 高级用法

#### 支持不同属性名

```csharp
public class MenuItem
{
    public string MenuId { get; set; }
    public string ParentMenuId { get; set; }
    public string MenuName { get; set; }
    public List<MenuItem> SubMenus { get; set; } = new List<MenuItem>();
}

// 使用自定义属性名
var menuTree = TreeHelper.ToTree(
    flatMenus,
    x => x.MenuId,           // 自定义ID属性
    x => x.ParentMenuId,     // 自定义父ID属性
    x => x.SubMenus,         // 自定义子节点属性
    (x, children) => x.SubMenus = children,
    "0"                      // 字符串类型的根ID
);
```

#### 支持不同数据类型

```csharp
public class Category
{
    public Guid CategoryId { get; set; }
    public Guid? ParentCategoryId { get; set; }
    public string CategoryName { get; set; }
    public IList<Category> SubCategories { get; set; } = new List<Category>();
}

// 使用Guid类型
var categoryTree = TreeHelper.ToTree(
    flatCategories,
    x => x.CategoryId,
    x => x.ParentCategoryId ?? Guid.Empty,
    x => x.SubCategories,
    (x, children) => x.SubCategories = children,
    Guid.Empty
);
```

## 性能特点

- **时间复杂度**: O(n) - 使用字典进行快速查找
- **空间复杂度**: O(n) - 需要额外的字典存储空间
- **非递归实现**: 避免栈溢出，适合深层树结构
- **深度优先遍历**: 保证数据顺序的一致性

## 支持的数据类型

| 类型 | 示例 | 说明 |
|------|------|------|
| `int` | `1, 2, 3` | 整数ID，最常用 |
| `string` | `"1", "2", "3"` | 字符串ID，支持复杂格式 |
| `Guid` | `Guid.NewGuid()` | GUID ID，全局唯一 |
| `long` | `1L, 2L, 3L` | 长整型ID |
| 其他 | 任何实现了相等比较的类型 | 自定义类型 |

## 最佳实践

### 1. 数据完整性检查

```csharp
// 转换前检查数据完整性
var orphanNodes = flatData.Where(x => 
    !Equals(x.ParentId, rootId) && 
    !flatData.Any(p => Equals(p.Id, x.ParentId))
).ToList();

if (orphanNodes.Any())
{
    Console.WriteLine($"发现孤立节点: {string.Join(", ", orphanNodes.Select(x => x.Id))}");
}
```

### 2. 性能优化

```csharp
// 对于大数据集，考虑分批处理
var batchSize = 1000;
var batches = flatData.Chunk(batchSize);
var allTrees = new List<TreeNode>();

foreach (var batch in batches)
{
    var batchTree = TreeHelper.ToTree(batch, ...);
    allTrees.AddRange(batchTree);
}
```

### 3. 错误处理

```csharp
try
{
    var treeData = TreeHelper.ToTree(flatData, ...);
}
catch (Exception ex)
{
    // 处理转换异常
    Console.WriteLine($"树形数据转换失败: {ex.Message}");
}
```

## 注意事项

1. **无需实现接口**: 完全兼容现有代码，无需修改任何类定义
2. **数据一致性**: 确保 `ParentId` 与某个节点的 `Id` 对应，或为根节点ID
3. **内存管理**: 转换过程会创建新对象，注意内存使用
4. **空值处理**: 委托函数必须正确处理 null 值情况
5. **循环引用**: 避免数据中存在循环引用，可能导致无限递归

## 相关文件

- `TreeHelper.cs` - 核心实现类
- `TreeExample.cs` - 详细使用示例
- `TreeHelperTests.cs` - 完整单元测试
- `TreeHelper.README.md` - 本文档

## 更新日志

- **v1.0.0** - 初始版本，支持基本的树形数据转换
- **v1.1.0** - 移除接口依赖，使用委托函数方式，提高兼容性
- **v1.2.0** - 添加树形数据遍历功能，完善文档和测试
