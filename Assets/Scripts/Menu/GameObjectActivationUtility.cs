using UnityEngine;

public static class GameObjectActivationUtility
{
    public static void SetActiveWithParents(GameObject target, bool active)
    {
        if (target == null)
            return;

        if (active)
            ActivateParents(target.transform);

        target.SetActive(active);
    }

    public static void SetOnlyChildActiveWithParents(GameObject target)
    {
        if (target == null)
            return;

        Transform parent = target.transform.parent;

        if (parent != null)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);

                if (child != target.transform)
                    child.gameObject.SetActive(false);
            }
        }

        ActivateParents(target.transform);
        target.SetActive(true);
    }

    private static void ActivateParents(Transform current)
    {
        if (current == null)
            return;

        if (current.parent != null)
            ActivateParents(current.parent);

        current.gameObject.SetActive(true);
    }
}
