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

  constructor(
    correlationId: string,
    agvId: string,
    x: number,
    y: number,
    rotation: number
  ) {
    super(agvId, { x, y })
    this.correlationId = correlationId
    this.icon.rotation(rotation)
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
      fillLinearGradientStartPoint: { x: 0, y: -6 },
      fillLinearGradientEndPoint: { x: 0, y: -5 },
      fillLinearGradientColorStops: [0, 'black', 1, 'red']
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
