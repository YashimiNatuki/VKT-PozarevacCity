using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pozarevac.CityGame
{
    // Renderer-only distance budget. Colliders, mission objects and simulation remain active.
    public sealed class CityMapVisibility : MonoBehaviour
    {
        private sealed class Entry{public Renderer renderer;public bool initial,visible;public float near,far;}
        private readonly List<Entry> entries=new List<Entry>(256);
        private Transform view;
        private Vector3 last;
        private float next;
        public int Registered=>entries.Count;
        public int VisibleCount{get{int total=0;foreach(var e in entries)if(e.renderer!=null&&e.renderer.enabled)total++;return total;}}
        public void Register(Renderer renderer,bool label)
        {
            if(renderer==null)throw new ArgumentNullException(nameof(renderer));
            entries.Add(new Entry{renderer=renderer,initial=renderer.enabled,visible=renderer.enabled,near=label?75:360,far=label?90:400});
        }
        public void Bind(Transform cameraView){view=cameraView;if(view!=null)ApplyAt(view.position);}
        public void ApplyAt(Vector3 position)
        {
            if(!float.IsFinite(position.x)||!float.IsFinite(position.y)||!float.IsFinite(position.z))throw new ArgumentOutOfRangeException(nameof(position));
            foreach(var e in entries)
            {
                if(e.renderer==null)continue;
                float radius=e.visible?e.far:e.near;
                e.visible=e.initial&&e.renderer.bounds.SqrDistance(position)<=radius*radius;e.renderer.enabled=e.visible;
            }
            last=position;
        }
        private void LateUpdate()
        {
            if(view==null)return;
            if(Time.unscaledTime<next&&(view.position-last).sqrMagnitude<32*32)return;
            next=Time.unscaledTime+.2f;ApplyAt(view.position);
        }
        private void OnDisable(){foreach(var e in entries)if(e.renderer!=null){e.renderer.enabled=e.initial;e.visible=e.initial;}}
    }
}
