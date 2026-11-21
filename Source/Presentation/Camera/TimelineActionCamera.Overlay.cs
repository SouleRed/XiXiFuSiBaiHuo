using CameraCue = Soule.MotionCamera.MotionCameraCue;
using UnityEngine;
namespace 裁缝之王.Presentation.Camera;

// 与当前演出框架相同的聚焦 FOV、轴向纵深、进入混合与画幅转换。
internal sealed partial class TimelineActionCamera
{
        // 源level12/BattleCamManager，ENCOUNTER聚焦配置；不包含BATTLE共同行动模式。
        // defaultZoom_Encounter=(38,80,50)。保留参数解释；距离扩框由图书馆承担。
        // 宽高比补偿固定使用源中性缩放值，事件输出不再反馈到参考镜头。
        const float SourceFocusedZoom=38f,SourceWideZoom=80f,SourceZoomLimiter=50f;
        // level12 defaultOffset_Encounter.z=-15. V2 offsets scale with the source map,
        // so their depth ratio is offset.z / 15; the map factor cancels on both sides.
        const float SourceFocusedDepth=15f;
        Vector3 CameraOffsetSample(CameraCue cue,float u,float blend)
        {
            return cue.axisCurves?new Vector3(Mathf.LerpUnclamped(cue.startOffset.x,cue.endOffset.x,cue.x.Evaluate(u)),Mathf.LerpUnclamped(cue.startOffset.y,cue.endOffset.y,cue.y.Evaluate(u)),Mathf.LerpUnclamped(cue.startOffset.z,cue.endOffset.z,cue.z.Evaluate(u))):Vector3.LerpUnclamped(cue.startOffset,cue.endOffset,blend);
        }
        // 每次求值独立生成，基础镜头与宽高比变化会在下一次求值生效。
        struct CameraReference
        {
            public Vector3 position,focus,forward;
            public float depth,aspectOffset,sourceFov,sourceHalfFovTangent;
        }
        CameraReference ReadCameraReference(Vector3 focusOffset)
        {
            var reference=new CameraReference {position=libraryFollowPosition+focusOffset,focus=nativeFocus+focusOffset,forward=libraryFollowRotation*Vector3.forward,aspectOffset=SourceAspectOffset};
            reference.depth=Mathf.Max(.0001f,Vector3.Dot(reference.focus-reference.position,reference.forward));
            reference.sourceFov=SourceFocusedFov(SourceFocusedZoom,reference);
            reference.sourceHalfFovTangent=Mathf.Tan(reference.sourceFov*.5f*Mathf.Deg2Rad);
            return reference;
        }
        Vector3 CameraWorldOffset(Vector3 offset,CameraReference reference,float sourcePullback)
        {
            float depthRatio=(offset.z-sourcePullback)/SourceFocusedDepth;
            return new Vector3(offset.x*actorDirection,offset.y,0f)*CameraEventUnits+reference.forward*(depthRatio*reference.depth);
        }
        static float CameraDepthRatio(Vector3 offset,CameraReference reference)
        {
            return Mathf.Max(.0001f,1f-Vector3.Dot(offset,reference.forward)/reference.depth);
        }

        // 使用源聚焦规则的最小参考跨度(横向5、纵深4)，不再次读取图书馆双方间距扩框。
        static readonly float SourceReferenceSpan=Mathf.Sqrt(5f*5f+4f*4f);
        float SourceAspectOffset {get{return Mathf.Max(2.223f-previewCamera.aspect,.445f)*2.5f*(SourceFocusedZoom/10f);}}
        static float SourceFocusedFov(float zoom,CameraReference reference)
        {
            float amount=Mathf.Clamp01(SourceReferenceSpan/SourceZoomLimiter);
            return Mathf.Clamp(Mathf.Lerp(zoom,SourceWideZoom,amount)+reference.aspectOffset,1f,179f);
        }
        // 正向与反向共用参考FOV，倍率与纵深的组合也保持同一顺序。
        static float SourceFrameRatio(float fov,CameraReference reference)
        {
            return Mathf.Tan(Mathf.Clamp(fov,1f,179f)*.5f*Mathf.Deg2Rad)/reference.sourceHalfFovTangent;
        }
        static float SourceZoomFromFrameRatio(float ratio,CameraReference reference)
        {
            float fov=2f*Mathf.Atan(reference.sourceHalfFovTangent*ratio)*Mathf.Rad2Deg;
            float amount=Mathf.Clamp01(SourceReferenceSpan/SourceZoomLimiter);
            return (fov-reference.aspectOffset-SourceWideZoom*amount)/(1f-amount);
        }
        static float CameraFrameScale(CameraCue cue,Vector3 worldOffset,CameraReference reference)
        {
            return cue.frameScale*CameraDepthRatio(worldOffset,reference);
        }
        void ReadSourceHeightFraming(CameraCue cue,out float worldLift,out float sourcePullback)
        {
            worldLift=sourcePullback=0f;
            if(!cue.v2||!cue.useHeight||cue.focusCenter)return;
            float height=SourceHeight(actor)*mapScale;
            worldLift=Mathf.Max(0f,height/20f-.2f)*CameraSourceUnits;
            sourcePullback=Mathf.Max(0f,height/10f-.1f)/Mathf.Max(.0001f,mapScale);
        }
        void PrepareSourceFocusedZoom(CameraCue cue)
        {
            float height=SourceHeight(cue.useHeight?actor:target);
            float low=Mathf.Clamp(height*mapScale,.833f*mapScale,2f*mapScale);
            float high=height*mapScale;
            float proportion=Mathf.Clamp01(Mathf.Max(1f,Mathf.Max(.833f*mapScale,low))/Mathf.Max(1f,high)*mapScale*.833f);
            sourceZoomBase=SourceFocusedZoom+Mathf.Max(-5f,10f-10f*proportion);
            float gain=.7f+.3f*proportion;
            sourceZoomGain=8f*gain*gain;
            if(!cue.currentStart)return;
            // 只在需要继承当前画幅时反算；与正向计算共用倍率和纵深换算。
            var reference=ReadCameraReference(Vector3.zero);
            ReadSourceHeightFraming(cue,out _,out float sourcePullback);
            Vector3 offset=cue.v2?CameraWorldOffset(CameraOffsetSample(cue,0f,Ease(cue.ease,0f,cue.curve)),reference,sourcePullback):Vector3.zero;
            sourceStartSize=(SourceZoomFromFrameRatio(overlayFrameRatio/CameraFrameScale(cue,offset,reference),reference)-sourceZoomBase)/sourceZoomGain;
        }
        Vector3 eventFocus,eventFocusVelocity,eventCameraOffset,entryOffset;
        float overlayFrameRatio=1f,entryAge,entryDuration,entryFrameRatio=1f,rotationFrameFrom=1f;
        float sourceStartSize,sourceZoomBase,sourceZoomGain;
        // 当帧构图结果直接返回；仅保留下一阶段插值需要的状态。
        struct CameraEvaluation
        {
            public Vector3 position;
            public float halfHeight,projectionDistance;
        }

        // V1: source zoom Tween -> target FOV -> native per-frame FOV smoothing.
        // Keep the Tween state separate from displayed FOV; a new V1 continues the current internal zoom.
        bool v1ZoomActive;
        float v1CurrentZoom,v1StartZoom,v1TargetZoom,v1DisplayedFov;
        void BeginV1Zoom(CameraCue cue)
        {
            if(cue.v2){v1ZoomActive=false;return;}
            if(!v1ZoomActive)
            {
                var reference=ReadCameraReference(eventFocus);
                v1CurrentZoom=SourceZoomFromFrameRatio(overlayFrameRatio/Mathf.Max(.0001f,cue.frameScale),reference);
                v1DisplayedFov=SourceFocusedFov(v1CurrentZoom,reference);
            }
            v1StartZoom=v1CurrentZoom;
            v1TargetZoom=cue.relative?sourceZoomBase+cue.size*sourceZoomGain:cue.size;
            v1ZoomActive=true;
        }
        Vector3 sourceRotateStart,sourceRotateCurrent,renderedRotate,rotateVelocity;
        float RotationBlend()
        {
            float duration=rotateCue.rotationDuration>=0f?rotateCue.rotationDuration:rotateCue.duration;
            return Ease(rotateCue.ease,rotateAge/Mathf.Max(.001f,duration),rotateCue.curve);
        }
        void AdvanceRenderedRotation(float dt)
        {
            if(dt<=0f)return;
            float smooth=Mathf.Max(.0001f,rotateCue.blend);
            renderedRotate.x=Mathf.SmoothDampAngle(renderedRotate.x,sourceRotateCurrent.x,ref rotateVelocity.x,smooth,Mathf.Infinity,dt);
            renderedRotate.y=Mathf.SmoothDampAngle(renderedRotate.y,sourceRotateCurrent.y,ref rotateVelocity.y,smooth,Mathf.Infinity,dt);
            renderedRotate.z=Mathf.SmoothDampAngle(renderedRotate.z,sourceRotateCurrent.z,ref rotateVelocity.z,smooth,Mathf.Infinity,dt);
        }
        void AdvanceV1Zoom(float blend,float dt)
        {
            v1CurrentZoom=Mathf.LerpUnclamped(v1StartZoom,v1TargetZoom,blend);
            float targetFov=SourceFocusedFov(v1CurrentZoom,ReadCameraReference(eventFocus));
            // 源 smoothTime_Zoom 是每秒插值系数，和镜头事件在同一子步推进。
            if(v1ZoomActive)v1DisplayedFov=Mathf.Lerp(v1DisplayedFov,targetFov,10f*dt);
        }

        void BeginCameraOverlay(CameraCue cue,bool nextPhase)
        {
            if(nextPhase)return; // 同一事件的第二阶段延续进入混合时钟。
            entryOffset=eventCameraOffset;entryFrameRatio=overlayFrameRatio;
            PrepareSourceFocusedZoom(cue);
            BeginV1Zoom(cue);
            entryAge=0f;entryDuration=cue.v2?Mathf.Max(0f,cue.entryBlend):0f;
        }

        CameraEvaluation EvaluateCameraOverlay(CameraCue cue,Vector3 offset,float zoomSample,float blend,float u)
        {
            float weight=1f;
            var reference=ReadCameraReference(eventFocus);
            Vector3 requested=Vector3.zero;
            if(cue.v2)
            {
                weight=entryDuration>0f?Ease(7,Mathf.Clamp01(entryAge/entryDuration),null):1f;
                ReadSourceHeightFraming(cue,out _,out float sourcePullback);
                requested=CameraWorldOffset(offset,reference,sourcePullback);
                eventCameraOffset=Vector3.LerpUnclamped(entryOffset,requested,weight);
            }
            Vector3 position=reference.position;
            if(cue.focusCenter)
            {
                // 显式中心构图移除基础镜头的画面偏置，保留原有沿镜头方向的距离。
                position=reference.focus-reference.forward*Vector3.Dot(reference.focus-position,reference.forward);
            }
            float referenceDistance=Mathf.Max(.0001f,Vector3.Distance(reference.focus,position));
            // Keep the calibrated neutral FOV, while actual forward translation reduces
            // the visible frame. Lateral offsets must not induce a compensating zoom.
            float projectionDistance=referenceDistance*CameraDepthRatio(eventCameraOffset,reference);
            position+=eventCameraOffset;
            float sourceFov;
            if(!cue.v2)sourceFov=v1DisplayedFov;
            else if(cue.useFov)sourceFov=Mathf.LerpUnclamped(cue.startFov,cue.endFov,cue.axisCurves?CameraValue(cue.fovCurve,u,u):blend);
            else
            {
                float zoom=cue.relative||cue.v2?sourceZoomBase+zoomSample*sourceZoomGain:zoomSample;
                sourceFov=SourceFocusedFov(zoom,reference);
            }
            // 先求出完整目标(含事件倍率)，只在此进行一次画幅进入混合。
            float targetFrameRatio=SourceFrameRatio(sourceFov,reference)*CameraFrameScale(cue,requested,reference);
            float ratio=Mathf.LerpUnclamped(entryFrameRatio,targetFrameRatio,weight);
            return new CameraEvaluation {position=position,halfHeight=Mathf.Max(.001f,libraryFollowSize*ratio),projectionDistance=projectionDistance};
        }


}
