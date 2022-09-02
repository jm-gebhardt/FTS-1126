import cytoscape from 'cytoscape'
import cyCanvas from 'cytoscape-canvas'
import { stylesheet } from '@/components/CytoscapeMap/stylesheet'
import { IAgvGraph, IEdge, INode } from '@/services/api/data-contracts'
import dblclick from 'cytoscape-dblclick'

if (!cytoscape('core', 'cyCanvas')) {
  cytoscape.use(cyCanvas)
}

if (!cytoscape('core', 'dblclick')) {
  cytoscape.use(dblclick)
}

/**
 * Cytoscape AGV Graph
 */
export class Map {
  cy: cytoscape.Core
  private _resolution = 1
  private _background: HTMLImageElement | null = null
  private _xOffset = 0
  private _yOffset = 0

  private readonly MinZoomLevel = 0.5
  private readonly MaxZoomLevel = 10

  /**
   * GraphMap Constructor
   * @param container: container where the map is rendered in
   */
  constructor(container: HTMLElement) {
    this.cy = cytoscape({
      container: container,
      autolock: true,
      style: stylesheet()
    })

    this.setCanvasProperties()
    this.registerEvents()
  }

  private setCanvasProperties(): void {
    this.cy.maxZoom(this.MaxZoomLevel)
    this.cy.minZoom(this.MinZoomLevel)
    this.cy.boxSelectionEnabled(false)
  }

  private registerEvents(): void {
    this.cy.on('dblclick', () => {
      this.showAll()
    })
  }

  /**
   * Show all nodes
   */
  showAll(): void {
    this.cy.viewport({
      zoom: 1,
      pan: {
        x: this._xOffset / this._resolution,
        y: this._yOffset / this._resolution
      }
    })
  }

  /**
   * Add AGV Graphs
   * All graphs will be merged into one to avoid duplicate nodes
   * @param graphs: Graphs
   */
  addGraphs(graphs: IAgvGraph[]): void {
    for (const graph of graphs) {
      const nodes = graph.nodes
      if (nodes != null) {
        this._addNodes(graph.correlationId, nodes)
      }
      const edges = graph.edges
      if (edges != null) {
        this._addEdges(graph.correlationId, edges)
      }
    }
  }

  /**
   * Remove all nodes and edges
   */
  removeAll(): void {
    this.cy.remove('node')
    this.cy.remove('edge')
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

    const bottomLayer = this.cy.cyCanvas({
      zIndex: -1,
      pixelRatio: 1
    })
    const canvas = bottomLayer.getCanvas()
    const ctx = canvas.getContext('2d')

    if (ctx == null) {
      return
    }

    this.cy.on('render cyCanvas.resize', () => {
      bottomLayer.resetTransform(ctx)
      bottomLayer.clear(ctx)
      bottomLayer.setTransform(ctx)

      ctx.save()
      if (this._background != null) {
        // Draw a background
        ctx.drawImage(
          this._background,
          (this._xOffset / this._resolution) * -1,
          (this._yOffset / this._resolution) * -1
        )
      }
    })
  }

  _addNodes(correlationId: string, nodes: INode[]): void {
    for (const node of nodes) {
      const element = this.cy.getElementById(node.id)
      if (element.id() != undefined) {
        element.data('agvs').push(correlationId)
      } else {
        this.cy.add({
          group: 'nodes',
          data: {
            id: node.id,
            nodeType: node.type,
            agvs: [correlationId]
          },
          position: {
            x: node.x / this._resolution,
            y: node.y / this._resolution
          }
        })
      }
    }
  }

  _addEdges(correlationId: string, edges: IEdge[]): void {
    for (const edge of edges) {
      const edgeId = `${edge.from}-${edge.to}`
      const element = this.cy.getElementById(edgeId)
      if (element.id() != undefined) {
        element.data('agvs').push(correlationId)
      } else {
        this.cy.add({
          group: 'edges',
          data: {
            id: `${edge.from}-${edge.to}`,
            source: edge.from,
            target: edge.to,
            agvs: [correlationId]
          }
        })
      }
    }
  }
}
