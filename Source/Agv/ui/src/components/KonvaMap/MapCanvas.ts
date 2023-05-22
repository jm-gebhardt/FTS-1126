import Konva from 'konva'
import {
  IAgv,
  IAgvPosition,
  IAgvPositionStatus,
  IAgvState,
  IEdge,
  IGraph,
  INode,
  INodeType
} from '@/services/api/data-contracts'
import {
  Charge,
  Edge,
  GraphDimensions,
  MapNode,
  Park,
  Station,
  WayPoint
} from './MapNodes'
import { Agv } from './MapAgv'
import { Vector2d } from 'konva/lib/types'

/**
 * Konva canvas for AGV Graph
 */
export class MapCanvas {
  imageLayer: Konva.Layer
  edgeLayer: Konva.Layer
  nodeLayer: Konva.Layer
  agvLayer: Konva.Layer

  private stage: Konva.Stage

  private _nodes: Map<string, MapNode> = new Map()
  private _edges: Map<string, Edge> = new Map()
  private _agvs: Map<string, Agv> = new Map()
  private _visibleAgvs: string[] = []

  private _background: HTMLImageElement | null = null
  private _image: Konva.Image | null = null

  private readonly ZoomFactor = 1.1 // change zoom by 10% for each scroll step
  private readonly MinZoomLevel = 0.1
  private readonly MaxZoomLevel = 10

  private menu = document.getElementById('menu')

  /**
   * MapCanvas Constructor
   * @param container: container where the map is rendered in
   */
  constructor(container: HTMLElement) {
    this.stage = new Konva.Stage({
      container: container.id,
      width: container.offsetWidth,
      height: container.offsetHeight,
      draggable: true
    })

    this.imageLayer = new Konva.Layer()
    this.edgeLayer = new Konva.Layer()
    this.nodeLayer = new Konva.Layer()
    this.agvLayer = new Konva.Layer()

    this.stage.add(this.imageLayer)
    this.stage.add(this.edgeLayer)
    this.stage.add(this.nodeLayer)
    this.stage.add(this.agvLayer)

    // Event handler for resizing stage on window resize
    window.addEventListener('resize', () => {
      if (container) {
        this.stage.width(container.offsetWidth)
        this.stage.height(container.offsetHeight)
        this.centerMap()
      }
    })

    // Event handler for zooming
    this.stage.on('wheel', (e) => {
      e.evt.preventDefault() // stop default scrolling

      const oldScale = this.stage.scaleX()
      const pointer = this.stage.getPointerPosition()
      if (pointer) {
        let direction = e.evt.deltaY // zoom direction

        // when using a trackpad, e.evt.ctrlKey is true, in that case invert direction
        if (e.evt.ctrlKey) {
          direction = -direction
        }

        let newScale = oldScale
        if (direction < 0) {
          newScale = oldScale * this.ZoomFactor
          if (newScale > this.MaxZoomLevel) newScale = this.MaxZoomLevel
        } else {
          newScale = oldScale / this.ZoomFactor
          if (newScale < this.MinZoomLevel) newScale = this.MinZoomLevel
        }

        const mousePointTo = {
          x: (pointer.x - this.stage.x()) / oldScale,
          y: (pointer.y - this.stage.y()) / oldScale
        }

        this.stage.scale({ x: newScale, y: newScale })
        this.stage.position({
          x: pointer.x - mousePointTo.x * newScale,
          y: pointer.y - mousePointTo.y * newScale
        })
      }
    })

    // closing of context menu when click on canvas
    const a = this.menu
    this.stage.on('click', function () {
      if (a) a.style.display = 'none'
    })
  }

  /**
   * Add AGV Graphs
   * All graphs will be merged into one to avoid duplicate nodes
   * @param graphs: Graphs
   */
  addGraphs(graphs: IGraph[]): void {
    for (const graph of graphs) {
      const nodes = graph.nodes
      if (nodes != null) {
        this._addNodes(nodes)
      }
      const edges = graph.edges
      if (edges != null) {
        this._addEdges(edges)
      }
    }

    this.centerMap()
  }

  _addNodes(nodes: INode[]): void {
    for (const node of nodes) {
      if (!this._nodes.has(node.id)) {
        let newNode: MapNode | null = null
        const radRotation: number = (node.theta * 180) / Math.PI

        switch (node.type) {
          case INodeType.Charge:
            newNode = new Charge(node.id, { x: node.x, y: node.y }, radRotation)
            break

          case INodeType.Park:
            newNode = new Park(node.id, { x: node.x, y: node.y }, radRotation)
            break

          case INodeType.Drop:
            newNode = new Station(
              node.id,
              { x: node.x, y: node.y },
              radRotation
            )
            break

          case INodeType.Pick:
            newNode = new Station(
              node.id,
              { x: node.x, y: node.y },
              radRotation
            )
            break

          case INodeType.PickDrop:
            newNode = new Station(
              node.id,
              { x: node.x, y: node.y },
              radRotation
            )
            break

          case INodeType.WayPoint:
            newNode = new WayPoint(
              node.id,
              { x: node.x, y: node.y },
              radRotation
            )
            break
        }

        if (newNode) {
          this._nodes.set(node.id, newNode)
          this.nodeLayer.add(newNode.tag)
          this.nodeLayer.add(newNode.icon)
        }
      }
    }
  }

  _addEdges(edges: IEdge[]): void {
    for (const edge of edges) {
      const edgeId = `${edge.from.id}-${edge.to.id}`

      if (!this._edges.has(edgeId)) {
        const posFrom = { x: edge.from.x, y: edge.from.y }
        const posTo = { x: edge.to.x, y: edge.to.y }

        const newEdge = new Edge(edgeId, posFrom, posTo)
        this._edges.set(edgeId, newEdge)
        this.edgeLayer.add(newEdge.line)
      }
    }
  }

  /**
   * Sets which AGVs are visible
   * @param agvIds: IDs of visible AGVs
   */
  setVisibleAgvs(agvCorrelationIds: string[]): void {
    this._visibleAgvs = agvCorrelationIds

    for (const agv of this._agvs.values()) {
      const visible = this._visibleAgvs.includes(agv.getCorrelationId())
      agv.icon.visible(visible)
      agv.tag.visible(visible)
    }
  }

  /**
   * Add AGV positions
   * @param agvs: Agv Positions
   */
  addAgvs(agvs: IAgvPositionStatus[]): void {
    for (const agv of agvs) {
      this.updateAgvPosition(agv)
    }
  }

  /**
   * Updates position of an AGV
   * @param agv: Agv Position
   */
  updateAgvPosition(agv: IAgvPositionStatus): void {
    const rotation = (agv.agvPosition.theta * 180) / Math.PI // Convert radians to degree: [-PI, PI] -> [-180°, 180°]

    let agvNode = this._agvs.get(agv.correlationId)

    if (agvNode) {
      agvNode.update({ x: agv.agvPosition.x, y: agv.agvPosition.y }, rotation)
    } else {
      agvNode = this.createAgv(agv.correlationId, agv.agvId, agv.agvPosition)
    }

    const visible = this._visibleAgvs.includes(agv.correlationId)
    agvNode.icon.visible(visible)
    agvNode.tag.visible(visible)
  }

  updateAgv(agv: IAgv): void {
    let agvNode = this._agvs.get(agv.correlationId)
    if (!agvNode) {
      agvNode = this.createAgv(agv.correlationId, agv.agvId)
    }

    let primary = '#FFFFFF' // white
    let secondary = undefined

    // Hardware states (priority over software state)
    if (agv.isOnline === false) {
      primary = '#808080' // grey
    } else if (agv.isDisturbed === true) {
      primary = 'FF8000' // darker orange
    } else if (agv.isAutomatic === false) {
      primary = '#0000FF' // blue
    } else if (agv.isCharging === true) {
      primary = '#FFFF00' /// yellow
    }
    // Software states
    else if (agv.currentState === IAgvState.Discovered) {
      primary = '#808080' // grey
    } else if (agv.currentState === IAgvState.Idle) {
      primary = '#FFFFFF' // white
    } else if (agv.currentState === IAgvState.Busy) {
      primary = '#008000' // green
    }
    secondary = agv.isLocked ? '#FF0000' : undefined
    agvNode.setColor(primary, secondary)
  }

  /**
   * Creates a new AGV ui element and adds it to the canvas layer
   * @param correlationId Unique identifier of AGV
   * @param agvId Name of AGV that is shown in the UI
   * @param agvPosition Optional. If not set AGV position/rotaion will be initialised to 0
   * @returns created agv
   */
  createAgv(
    correlationId: string,
    agvId: string,
    agvPosition?: IAgvPosition
  ): Agv {
    let x = 0
    let y = 0
    let rotation = 0

    if (agvPosition) {
      x = agvPosition.x
      y = agvPosition.y
      rotation = (agvPosition.theta * 180) / Math.PI // Convert radians to degree: [-PI, PI] -> [-180°, 180°]
    }

    const agv = new Agv(correlationId, agvId, x, y, rotation)

    this._agvs.set(correlationId, agv)
    this.agvLayer.add(agv.icon)
    this.agvLayer.add(agv.tag)

    return agv
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
    offsetX: number,
    offsetY: number
  ): void {
    this._background = background

    // Remember current dimensions for rescale
    const oldScale = GraphDimensions.scale
    const oldOffsetX = GraphDimensions.offsetX
    const oldOffsetY = GraphDimensions.offsetY

    if (resolution === 0) {
      resolution = 1 // Avoid 'division by zero' error
    }

    GraphDimensions.scale = 1 / resolution // Inverse of resolution
    GraphDimensions.offsetX = offsetX / resolution
    GraphDimensions.offsetY = offsetY / resolution

    this._image = new Konva.Image({
      image: this._background
    })
    this.imageLayer.destroyChildren()
    this.imageLayer.add(this._image)

    // Check if scale has changed
    if (
      GraphDimensions.scale != oldScale ||
      GraphDimensions.offsetX != oldOffsetX ||
      GraphDimensions.offsetY != oldOffsetY
    ) {
      // Rescale all map opjects to set new scale
      this._nodes.forEach((n) => n.rescale(oldScale, oldOffsetX, oldOffsetY))
      this._edges.forEach((e) => e.rescale(oldScale, oldOffsetX, oldOffsetY))
      this._agvs.forEach((a) => a.rescale(oldScale, oldOffsetX, oldOffsetY))
    }

    this.centerMap()
  }

  centerMap(): void {
    if (this._image == null || this._nodes.size == 0) {
      return
    }

    const nodes = Array.from<MapNode>(this._nodes.values())

    const minX = this.getMin(nodes, (m) => m.x)
    const maxX = this.getMax(nodes, (m) => m.x)
    const minY = this.getMin(nodes, (m) => m.y)
    const maxY = this.getMax(nodes, (m) => m.y)

    const graphHeigth = maxY - minY
    const graphWidth = maxX - minX

    // height of the visible area of Konva
    const viewHeight = this.stage.getSize().height
    const viewWidth = this.stage.getSize().width

    // x percentage margin
    const margin = 1.4
    const scaleByY = viewHeight / (graphHeigth * margin)
    const scaleByX = viewWidth / (graphWidth * margin)

    // Take the minimum scale so that zoomed out far enough that the last two edges (vertically or horizontally) are resting against the frame
    const scale = Math.min(scaleByX, scaleByY)
    this.stage.scaleX(scale)
    this.stage.scaleY(scale)

    const offsetX = -viewWidth / scale / 2 + minX + graphWidth / 2
    const offsetY = -viewHeight / scale / 2 + minY + graphHeigth / 2
    this.stage.offsetX(offsetX)
    this.stage.offsetY(offsetY)
  }

  private getMin(
    nodes: MapNode[],
    selector: (value: Vector2d) => number
  ): number {
    return Math.min(...this.map(nodes, selector))
  }

  private getMax(
    nodes: MapNode[],
    selector: (value: Vector2d) => number
  ): number {
    return Math.max(...this.map(nodes, selector))
  }

  private map(
    nodes: MapNode[],
    selector: (value: Vector2d) => number
  ): number[] {
    return nodes.map((m) => selector(m.getPosition()))
  }

  /**
   * Remove all nodes and edges
   */
  clearGraph(): void {
    this._nodes.clear()
    this._edges.clear()
    this.edgeLayer.destroyChildren()
    this.nodeLayer.destroyChildren()
  }
}
