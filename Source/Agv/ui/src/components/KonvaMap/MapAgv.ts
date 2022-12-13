import Konva from 'konva'
import { Vector2d } from 'konva/lib/types'
import { GraphDimensions, MapNode } from './MapNodes'

/**
 * AGV node
 */
export class Agv extends MapNode {
  shapeTween?: Konva.Tween // Animation object for shape
  tagTween?: Konva.Tween // Animation object for tag

  private lastUpdate = performance.now()
  private correlationId: string
  private agvId: string
  private start: number
  private previousTimeStamp: number
  private runAnimation: boolean
  private reverseAnimation: boolean

  constructor(
    correlationId: string,
    agvId: string,
    x: number,
    y: number,
    rotation: number
  ) {
    super(agvId, { x, y }, rotation)
    this.correlationId = correlationId
    this.agvId = agvId
    this.icon.rotation(rotation)
    this.start = -1
    this.previousTimeStamp = -1
    this.runAnimation = false
    this.reverseAnimation = false
    this.updateOnClickAgv(correlationId)
  }

  /**
   * Init Konva.Shape for AGV icon
   */
  initIcon(): Konva.Shape {
    return new Konva.RegularPolygon({
      sides: 3,
      radius: 10,
      stroke: 'black',
      strokeWidth: 1,
      fill: 'grey'
    })
  }

  updateOnClickAgv(correlationId: string): void {
    this.icon.on('click', function () {
      window.location.replace(
        import.meta.env.VITE_BASE_PATH + 'agvs?correlationId=' + correlationId
      )
    })
  }

  /**
   * Get correlationId of this AGV
   */
  getCorrelationId(): string {
    return this.correlationId
  }

  /**
   * Use Tween animations for smoother movement
   */
  updateSmooth(position: Vector2d, rotation: number): void {
    this.shapeTween?.finish()
    this.tagTween?.finish()

    this.shapeTween?.destroy()
    this.tagTween?.destroy()

    const now = performance.now()
    const duration = (now - this.lastUpdate) / 1000
    this.lastUpdate = now

    const x = position.x * GraphDimensions.scale + GraphDimensions.offsetX
    const y = position.y * GraphDimensions.scale + GraphDimensions.offsetY

    // Fix for wrong rotation direction
    const rotationDiff = this.icon.rotation() - rotation
    if (rotationDiff > 180) {
      this.icon.rotation(this.icon.rotation() - 360)
    } else if (rotationDiff < -180) {
      this.icon.rotation(this.icon.rotation() + 360)
    }

    this.shapeTween = new Konva.Tween({
      node: this.icon,
      x: x,
      y: y,
      rotation: rotation,
      duration: duration,
      easing: Konva.Easings.Linear
    })

    this.tagTween = new Konva.Tween({
      node: this.tag,
      x: x - this.tag.width() / 2,
      y: y + this.tagOffsetY,
      duration: duration,
      easing: Konva.Easings.Linear
    })

    this.shapeTween.play()
    this.tagTween.play()
  }
}
