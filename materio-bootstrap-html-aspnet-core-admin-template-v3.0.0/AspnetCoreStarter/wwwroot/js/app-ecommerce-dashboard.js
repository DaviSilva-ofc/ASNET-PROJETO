/**
 * eCommerce Dashboards
 */

'use strict';

(function () {
  let cardColor,
    headingColor,
    labelColor,
    fontFamily,
    borderColor,
    legendColor,
    heatMap1,
    heatMap2,
    heatMap3,
    heatMap4,
    currentTheme,
    chartBgColor;

  if (isDarkStyle) {
    heatMap1 = '#333457';
    heatMap2 = '#3c3e75';
    heatMap3 = '#484b9b';
    heatMap4 = '#696cff';
    chartBgColor = '#474360';
    currentTheme = 'dark';
  } else {
    heatMap1 = '#ededff';
    heatMap2 = '#d5d6ff';
    heatMap3 = '#b7b9ff';
    heatMap4 = '#696cff';
    chartBgColor = '#F0F2F8';
    currentTheme = 'light';
  }
  cardColor = config.colors.cardColor;
  headingColor = config.colors.headingColor;
  labelColor = config.colors.textMuted;
  borderColor = config.colors.borderColor;
  legendColor = config.colors.bodyColor;
  fontFamily = config.fontFamily;

  // Chart Colors
  const chartColors = {
    donut: {
      series1: config.colors.primary,
      series2: '#9055fdb3',
      series3: '#9055fd80'
    },
    donut2: {
      series1: '#49AC00',
      series2: '#4DB600',
      series3: config.colors.success,
      series4: '#78D533',
      series5: '#9ADF66',
      series6: '#BBEA99'
    },
    line: {
      series1: config.colors.warning,
      series2: config.colors.primary,
      series3: '#7367f029'
    }
  };

  // Total Profit Chart
  // --------------------------------------------------------------------
  const totalProfitChartEl = document.querySelector('#totalProfitChart'),
    totalProfitChartConfig = {
      chart: {
        type: 'bar',
        height: 260,
        parentHeightOffset: 0,
        stacked: true,
        toolbar: {
          show: false
        }
      },
      series: [
        {
          name: 'Revenue',
          data: [29, 22, 25, 19, 29, 20, 35]
        },
        {
          name: 'Transactions',
          data: ['', 16, 11, 16, '', 13, 10]
        },
        {
          name: 'Total Profit',
          data: ['', '', '', 14, '', 12, 12]
        }
      ],
      plotOptions: {
        bar: {
          horizontal: false,
          columnWidth: '35%',
          borderRadius: 8,
          borderRadiusApplication: 'around',
          startingShape: 'rounded',
          endingShape: 'rounded'
        }
      },
      dataLabels: { enabled: false },
      stroke: {
        curve: 'smooth',
        width: 6,
        lineCap: 'round',
        colors: [cardColor]
      },
      legend: { show: false },
      colors: [config.colors.primary, config.colors.success, config.colors.secondary],
      grid: {
        xaxis: { lines: { show: false } },
        strokeDashArray: 8,
        borderColor,
        padding: {
          top: -10,
          left: 15,
          right: -15,
          bottom: -10
        }
      },
      xaxis: {
        axisTicks: { show: false },
        crosshairs: { opacity: 0 },
        axisBorder: { show: false },
        categories: ['2015', '2016', '2017', '2018', '2019', '2020', '2021'],
        tickPlacement: 'on',
        labels: {
          style: {
            fontSize: '13px',
            fontFamily: fontFamily,
            colors: labelColor
          }
        }
      },
      yaxis: {
        labels: {
          formatter: function (val) {
            return parseInt(val) + 'K';
          },
          style: {
            fontSize: '13px',
            fontFamily: fontFamily,
            colors: labelColor
          }
        }
      },
      states: {
        hover: { filter: { type: 'none' } },
        active: { filter: { type: 'none' } }
      },
      responsive: [
        {
          breakpoint: 1441,
          options: {
            plotOptions: {
              bar: {
                columnWidth: '50%'
              }
            }
          }
        },
        {
          breakpoint: 1200,
          options: {
            plotOptions: {
              bar: {
                columnWidth: '35%'
              }
            }
          }
        },
        {
          breakpoint: 1025,
          options: {
            plotOptions: {
              bar: {
                columnWidth: '45%'
              }
            }
          }
        },
        {
          breakpoint: 767,
          options: {
            plotOptions: {
              bar: {
                columnWidth: '35%'
              }
            }
          }
        },
        {
          breakpoint: 555,
          options: {
            plotOptions: {
              bar: {
                columnWidth: '45%'
              }
            }
          }
        },
        {
          breakpoint: 450,
          options: {
            chart: {
              height: 200,
              offsetX: -10
            },
            plotOptions: {
              bar: {
                columnWidth: '55%'
              }
            },
            xaxis: {
              labels: {
                rotate: 315,
                rotateAlways: true
              }
            }
          }
        },
        {
          breakpoint: 360,
          options: {
            plotOptions: {
              bar: {
                columnWidth: '75%'
              }
            }
          }
        }
      ]
    };
  if (typeof totalProfitChartEl !== undefined && totalProfitChartEl !== null) {
    const totalProfitChart = new ApexCharts(totalProfitChartEl, totalProfitChartConfig);
    totalProfitChart.render();
  }
  // Total Sales Donut Chart
  // --------------------------------------------------------------------
  const totalSalesDonutChartEl = document.querySelector('#totalSalesDonutChart'),
    totalSalesDonutChartConfig = {
      chart: {
        height: 100,
        width: 110,
        parentHeightOffset: 0,
        type: 'donut'
      },
      labels: ['Comments', 'Replies', 'Shares'],
      series: [45, 10, 18, 27],
      colors: [config.colors.primary, config.colors.info, config.colors.warning, config.colors.danger],
      stroke: {
        width: 5,
        colors: cardColor
      },
      tooltip: {
        show: false,
        theme: currentTheme
      },
      dataLabels: {
        enabled: false,
        formatter: function (val, opt) {
          return parseInt(val) + '%';
        }
      },
      grid: {
        padding: {
          top: -10,
          right: -10,
          left: -10,
          bottom: -5
        }
      },
      legend: {
        show: false
      },
      plotOptions: {
        pie: {
          donut: {
            size: '75%',
            labels: {
              show: true,
              value: {
                fontSize: '1.15rem',
                fontFamily: 'Inter',
                color: headingColor,
                fontWeight: 500,
                offsetY: -18,
                formatter: function (val) {
                  return parseInt(val) + '%';
                }
              },
              name: {
                offsetY: 18,
                fontFamily: 'Inter'
              },
              total: {
                label: '',
                show: true,
                fontSize: '0.75rem',
                label: '1 Quarter',
                color: legendColor,
                fontWeight: 400,
                formatter: function (w) {
                  return '28%';
                }
              }
            }
          }
        }
      },
      states: {
        hover: {
          filter: {
            type: 'none'
          }
        },
        active: {
          filter: {
            type: 'none'
          }
        }
      }
    };
  if (typeof totalSalesDonutChartEl !== undefined && totalSalesDonutChartEl !== null) {
    const totalSalesDonutChart = new ApexCharts(totalSalesDonutChartEl, totalSalesDonutChartConfig);
    totalSalesDonutChart.render();
  }

  // Total Revenue chart
  // --------------------------------------------------------------------
  const totalRevenueChartEl = document.querySelector('#totalRevenueChart'),
    totalRevenueChartConfig = {
      chart: {
        height: 80,
        type: 'line',
        parentHeightOffset: 0,
        toolbar: {
          show: false
        },
        dropShadow: {
          enabled: true,
          color: [config.colors.primary],
          top: 12,
          left: 0,
          blur: 3,
          opacity: 0.1
        }
      },
      grid: {
        show: false,
        xaxis: {
          lines: {
            show: false
          }
        },
        yaxis: {
          lines: {
            show: false
          }
        },
        padding: {
          top: -15,
          left: -7,
          right: 9,
          bottom: -15
        }
      },
      colors: [config.colors.primary],
      stroke: {
        width: 5,
        curve: 'smooth'
      },
      series: [
        {
          data: [13, 30, 20, 35]
        }
      ],
      tooltip: {
        shared: false,
        intersect: true,
        x: {
          show: false
        }
      },
      xaxis: {
        labels: {
          show: false
        },
        axisTicks: {
          show: false
        },
        axisBorder: {
          show: false
        }
      },
      yaxis: {
        labels: {
          show: false
        }
      },
      tooltip: {
        enabled: false
      },
      markers: {
        size: 7,
        strokeColors: 'transparent',
        strokeWidth: 5,
        offsetX: -3,
        colors: ['transparent'],
        discrete: [
          {
            seriesIndex: 0,
            dataPointIndex: 3,
            fillColor: cardColor,
            strokeColor: config.colors.primary,
            size: 7,
            shape: 'circle'
          }
        ],
        hover: {
          size: 7
        }
      },
      responsive: [
        {
          breakpoint: 1354,
          options: {
            chart: {
              height: 80
            },
            markers: {
              strokeWidth: 4
            }
          }
        },
        {
          breakpoint: 1200,
          options: {
            chart: {
              height: 100
            }
          }
        },
        {
          breakpoint: 840,
          options: {
            chart: {
              height: 80
            }
          }
        },
        {
          breakpoint: 768,
          options: {
            chart: {
              height: 110
            }
          }
        }
      ]
    };
  if (typeof totalRevenueChartEl !== undefined && totalRevenueChartEl !== null) {
    const totalRevenueChart = new ApexCharts(totalRevenueChartEl, totalRevenueChartConfig);
    totalRevenueChart.render();
  }

  // Total Sales Semi Donut chart
  // --------------------------------------------------------------------
  const totalSalesSemiDonutChartEl = document.querySelector('#totalSalesSemiDonutChart'),
    totalSalesSemiDonutChartConfig = {
      chart: {
        height: 140,
        type: 'radialBar',
        sparkline: {
          enabled: true
        }
      },
      plotOptions: {
        radialBar: {
          hollow: {
            size: '65%'
          },
          startAngle: -90,
          endAngle: 90,
          track: {
            background: chartBgColor
          },
          dataLabels: {
            name: {
              show: false
            },
            value: {
              offsetY: -2,
              fontSize: '1.25rem',
              fontWeight: 500,
              fontFamily: 'Inter',
              color: legendColor
            }
          }
        }
      },
      states: {
        hover: {
          filter: {
            type: 'none'
          }
        },
        active: {
          filter: {
            type: 'none'
          }
        }
      },
      stroke: {
        lineCap: 'round'
      },
      colors: [config.colors.info],
      series: [78],
      labels: ['Progress'],
      responsive: [
        {
          breakpoint: 1600,
          options: {
            chart: {
              height: 160
            }
          }
        },
        {
          breakpoint: 1500,
          options: {
            chart: {
              height: 120
            }
          }
        },
        {
          breakpoint: 1200,
          options: {
            chart: {
              height: 180
            }
          }
        },
        {
          breakpoint: 840,
          options: {
            chart: {
              height: 140
            }
          }
        },
        {
          breakpoint: 768,
          options: {
            chart: {
              height: 180
            }
          }
        }
      ]
    };

  if (typeof totalSalesSemiDonutChartEl !== undefined && totalSalesSemiDonutChartEl !== null) {
    const totalSalesSemiDonutChart = new ApexCharts(totalSalesSemiDonutChartEl, totalSalesSemiDonutChartConfig);
    totalSalesSemiDonutChart.render();
  }

  // New Visitors Chart
  // --------------------------------------------------------------------
  const newVisitorsChartEl = document.querySelector('#newVisitorsChart'),
    newVisitorsChartConfig = {
      chart: {
        height: 164,
        type: 'bar',
        parentHeightOffset: 0,
        toolbar: {
          show: false
        }
      },
      plotOptions: {
        bar: {
          borderRadius: 5,
          distributed: true,
          columnWidth: '60%',
          endingShape: 'rounded',
          startingShape: 'rounded'
        }
      },
      series: [
        {
          data: [38, 55, 48, 65, 80, 38, 48]
        }
      ],
      tooltip: {
        enabled: false
      },
      legend: {
        show: false
      },
      dataLabels: {
        enabled: false
      },
      colors: [
        config.colors_label.primary,
        config.colors_label.primary,
        config.colors_label.primary,
        config.colors_label.primary,
        config.colors.primary,
        config.colors_label.primary,
        config.colors_label.primary
      ],
      grid: {
        show: false,
        padding: {
          left: -10,
          top: -10
        }
      },
      states: {
        hover: {
          filter: {
            type: 'none'
          }
        },
        active: {
          filter: {
            type: 'none'
          }
        }
      },
      xaxis: {
        show: false,
        axisTicks: {
          show: false
        },
        axisBorder: {
          show: false
        },
        labels: {
          show: false
        }
      },
      yaxis: { show: false },
      responsive: [
        {
          breakpoint: 1375,
          options: {
            chart: {
              height: 130
            }
          }
        },
        {
          breakpoint: 768,
          options: {
            chart: {
              height: 150
            }
          }
        }
      ]
    };
  if (typeof newVisitorsChartEl !== undefined && newVisitorsChartEl !== null) {
    const newVisitorsChart = new ApexCharts(newVisitorsChartEl, newVisitorsChartConfig);
    newVisitorsChart.render();
  }

  // Website Statistic
  const webVisitorsEl = document.querySelector('#webVisitors'),
    webVisitorsConfig = {
      chart: {
        height: 90,
        width: 160,
        parentHeightOffset: 0,
        type: 'bar',
        toolbar: {
          show: false
        }
      },
      plotOptions: {
        bar: {
          barHeight: '85%',
          columnWidth: '35%',
          startingShape: 'rounded',
          endingShape: 'rounded',
          borderRadius: 3,
          distributed: true
        }
      },
      colors: [config.colors.primary],
      grid: {
        padding: {
          top: -40,
          left: -12
        },
        yaxis: { lines: { show: false } }
      },
      dataLabels: {
        enabled: false
      },
      series: [
        {
          data: [50, 40, 130, 100, 75, 100, 45, 35]
        }
      ],
      tooltip: {
        enabled: false
      },
      legend: {
        show: false
      },
      xaxis: {
        labels: {
          show: false
        },
        axisTicks: {
          show: false
        },
        axisBorder: {
          show: false
        }
      },
      yaxis: {
        labels: {
          show: false
        }
      }
    };
  if (typeof webVisitorsEl !== undefined && webVisitorsEl !== null) {
    const webVisitors = new ApexCharts(webVisitorsEl, webVisitorsConfig);
    webVisitors.render();
  }
})();
