# UnityGUIArrayDrawer
Fancy Unity Array drawer

```csharp
var days = new List<DayModel>(dayModels);
EditorGUI.BeginChangeCheck();
daySelectedIndex = GUIArrayDrawer.DrawList<DayModel>("days", "Days", days, DrawDay, null, false);
if(EditorGUI.EndChangeCheck())
{
    dayModels = days;
}
void DrawDay(DayModel day)
{
    EditorGUILayout.LabelField($"Day {day.dayNum}  ({day.taskContainer.Select(_ => _.cost).Sum()} ☆)");
}
```


https://user-images.githubusercontent.com/79690923/161947191-6d05cd78-103c-4589-bc0e-ca3167f60c9b.mp4

