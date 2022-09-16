import Konva from 'konva'
import { IAgvPosition } from '@/services/api/data-contracts'
import { Vector2d } from 'konva/lib/types'

class AgvData {
  tag: Konva.Text
  shape: Konva.RegularPolygon

  private tagOffsetY = -32 // Experimentally determined

  constructor(
    agvLayer: Konva.Layer,
    agvId: string,
    x: number,
    y: number,
    rotation: number
  ) {
    this.tag = new Konva.Text({
      text: agvId
    })

    this.shape = new Konva.RegularPolygon({
      x: x,
      y: y,
      rotation: rotation,
      sides: 3,
      radius: 20,
      stroke: 'black',
      strokeWidth: 1,
      fillLinearGradientStartPoint: { x: 0, y: 0 },
      fillLinearGradientEndPoint: { x: 0, y: 1 },
      fillLinearGradientColorStops: [0, 'red', 1, 'blue']
    })

    this.tag.position({
      x: x - this.tag.width() / 2,
      y: y + this.tagOffsetY
    })

    agvLayer.add(this.shape)
    agvLayer.add(this.tag)
  }

  Update(position: Vector2d, rotation: number): void {
    this.tag.position({
      x: position.x - this.tag.width() / 2,
      y: position.y + this.tagOffsetY
    })
    this.shape.position(position)
    this.shape.rotation(rotation)
  }
}

/**
 * Konva AGV Graph
 */
export class MapCanvas {
  mapLayer: Konva.Layer
  agvLayer: Konva.Layer

  private _agvs: Map<string, AgvData> = new Map()
  private _background: HTMLImageElement | null = null
  private _resolution = 1
  private _width = 0
  private _height = 0
  private _xOffset = 0
  private _yOffset = 0

  private readonly MinZoomLevel = 0.5
  private readonly MaxZoomLevel = 10

  /**
   * GraphMap Constructor
   * @param container: container where the map is rendered in
   */
  constructor(container: HTMLElement) {
    this._width = container.offsetWidth
    this._height = container.offsetHeight

    const stage = new Konva.Stage({
      container: container.id,
      width: this._width,
      height: this._height
    })

    this.mapLayer = new Konva.Layer()
    this.agvLayer = new Konva.Layer()

    stage.add(this.mapLayer)
    stage.add(this.agvLayer)
  }

  /**
   * Add AGV positions
   * @param agvs: Agv Positions
   */
  addAgvs(agvs: IAgvPosition[]): void {
    for (const agv of agvs) {
      this.UpdateAgvPosition(agv)
    }
  }

  /**
   * Updates position of an AGV
   * @param agv: Agv Position
   */
  UpdateAgvPosition(agv: IAgvPosition): void {
    const scale = 40
    const offset = 80

    const x = agv.x * scale + offset
    const y = agv.y * scale + offset
    const r = (agv.theta * 180) / Math.PI // Convert radians to degree: [-PI, PI] -> [-180°, 180°]

    if (!this._agvs.has(agv.agvId)) {
      const data = new AgvData(this.agvLayer, agv.agvId, x, y, r)
      this._agvs.set(agv.agvId, data)
    }

    const element = this._agvs.get(agv.agvId)
    element?.Update({ x, y }, r)
  }

  /**
   * Set background properties
   * Call renderBackground to render
   * @param background
   * @param resolution
   */
  setBackground(
    background: HTMLImageElement,
    resolution: number,
    xOffset: number,
    yOffset: number
  ): void {
    this._background = background
    this._resolution = resolution
    this._xOffset = xOffset
    this._yOffset = yOffset
  }

  /**
   * Render background map
   */
  renderBackground(): void {
    if (this._background == null) {
      return
    }

    const scale = 1
    const image = new Konva.Image({
      image: this._background,
      x: this._xOffset,
      y: this._yOffset,
      scale: { x: scale, y: scale }
    })

    this.mapLayer.add(image)
  }

  /**
   * Show all nodes
   */
  showAll(): void {
    this.renderBackground()
  }

  /**
   * Remove all agvs, nodes and edges
   */
  clear(): void {
    this._agvs.clear()
    this.agvLayer.destroyChildren()
  }
}
