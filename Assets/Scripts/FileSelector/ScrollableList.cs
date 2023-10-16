// from https://www.youtube.com/watch?v=TRLsmuYMs8Q

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class ScrollableList : MonoBehaviour
{
    public GameObject itemPrefab;
    public int columnCount = 1;

    private int itemCount;

    public void CreateButtons(string[] folders )
    {
        foreach (Transform child in gameObject.transform)
        {
            Destroy(child.gameObject);
        }

        itemCount = folders.Length;

        if (itemCount == 0)
            return;

        RectTransform rowRectTransform = itemPrefab.GetComponent<RectTransform>();
        RectTransform containerRectTransform = gameObject.GetComponent<RectTransform>();

        //calculate the width and height of each child item.
        float width = containerRectTransform.rect.width / columnCount;
        float ratio = width / rowRectTransform.rect.width;
        float height = rowRectTransform.rect.height * ratio;
        int rowCount = itemCount / columnCount;
        if (rowCount != 0)
        {
            if (itemCount % rowCount > 0)
                rowCount++;
        }

        //adjust the height of the container so that it will just barely fit all its children
        float scrollHeight = height * rowCount;
        //containerRectTransform.offsetMin = new Vector2(containerRectTransform.offsetMin.x, -scrollHeight / 2);
        //containerRectTransform.offsetMax = new Vector2(containerRectTransform.offsetMax.x, scrollHeight / 2);
        containerRectTransform.offsetMin = new Vector2(containerRectTransform.offsetMin.x, -scrollHeight);
        containerRectTransform.offsetMax = new Vector2(containerRectTransform.offsetMax.x, 0f);

        int j = 0;
        for (int i = 0; i < itemCount; i++)
        {
            //this is used instead of a double for loop because itemCount may not fit perfectly into the rows/columns
            if (i % columnCount == 0)
                j++;

            //create a new item, name it, and set the parent
            GameObject newItem = Instantiate(itemPrefab) as GameObject;
            newItem.name = gameObject.name + " item at (" + i + "," + j + ")";
            newItem.transform.SetParent(gameObject.transform);
            newItem.transform.localScale = Vector3.one;

            newItem.GetComponentInChildren<Text>().text = folders[i];

            //move and size the new item
            RectTransform rectTransform = newItem.GetComponent<RectTransform>();

            float x = -containerRectTransform.rect.width / 2 + width * (i % columnCount);
            float y = containerRectTransform.rect.height / 2 - height * j;
            rectTransform.offsetMin = new Vector2(x, y);

            x = rectTransform.offsetMin.x + width;
            y = rectTransform.offsetMin.y + height;
            rectTransform.offsetMax = new Vector2(x, y);
        }

    }

}
