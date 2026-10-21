using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// Pointer events work for both a mouse and a finger. A missed drop never advances the story.
public class LetterDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
 public RectTransform destination;
 public Action completed;
 public Action<Vector2,bool> handMoved;
 public Action rejected;
 RectTransform rect; Vector2 home,offset; bool dragging,done,selected;
 public bool IsDone => done;
 void Awake(){rect=(RectTransform)transform;home=rect.anchoredPosition;}
 public void OnBeginDrag(PointerEventData e){if(done)return;dragging=true;selected=false;home=rect.anchoredPosition;RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)rect.parent,e.position,e.pressEventCamera,out var p);offset=rect.anchoredPosition-p;transform.SetAsLastSibling();}
 public void OnDrag(PointerEventData e){if(!dragging||done)return;RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)rect.parent,e.position,e.pressEventCamera,out var p);var next=p+offset;var bounds=((RectTransform)rect.parent).rect;next.x=Mathf.Clamp(next.x,0,Mathf.Max(0,bounds.width-rect.sizeDelta.x));next.y=Mathf.Clamp(next.y,-Mathf.Max(0,bounds.height-rect.sizeDelta.y),0);rect.anchoredPosition=next;handMoved?.Invoke(rect.anchoredPosition,true);}
 public void OnEndDrag(PointerEventData e){if(!dragging||done)return;dragging=false;if(RectTransformUtility.RectangleContainsScreenPoint(destination,e.position,e.pressEventCamera))Accept();else{rect.anchoredPosition=home;handMoved?.Invoke(home,false);rejected?.Invoke();}}
 public void OnPointerClick(PointerEventData e){if(done||e.dragging)return;selected=true;GetComponent<Graphic>().color=new Color(1,.9f,.7f);}
 public void TapDestination(){if(selected&&!done)Accept();}
 public void Accept(){if(done)return;done=true;rect.anchoredPosition=(Vector2)rect.parent.InverseTransformPoint(destination.position)+(destination.sizeDelta.x<100?Vector2.zero:new Vector2(12,-12));handMoved?.Invoke(rect.anchoredPosition,true);completed?.Invoke();}
}
